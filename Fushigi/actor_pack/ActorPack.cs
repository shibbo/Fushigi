using Fasterflect;
using Fushigi.actor_pack.components;
using Fushigi.Bfres;
using Fushigi.Byml.Serializer;
using Fushigi.gl.Bfres;
using Fushigi.SARC;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fushigi
{
    public class ActorPackCache
    {
        public static Dictionary<string, ActorPack> Actors = new Dictionary<string, ActorPack>();

        public static ActorPack Load(string gyml)
        {
            string path = FileUtil.FindContentPath(Path.Combine("Pack", "Actor", $"{gyml}.pack.zs"));
            if (!File.Exists(path))
                return null;

            if (!Actors.ContainsKey(gyml))
                Actors.Add(gyml, new ActorPack(path));

            return Actors[gyml];
        }
    }

    public class ActorPack
    {
        Dictionary<string, ActorParam> paramTree = [];
        public ModelInfo DrawArrayModelInfoRef;
        public ModelInfo ModelInfoRef;
        public ModelExpandParam ModelExpandParamRef;
        public DrainPipe DrainPipeRef;
        public GamePhysics GamePhysicsRef;
        public ControllerSetParam ControllerPath;
        public ShapeParamList ShapeParams;
        public BgUnitInfo BgUnitInfo;

        public string Category = "";

        public ActorPack(string path)
        {
            try
            {
                Load(path);
            }
            catch
            {

            }
        }

        public string GetModelName()
        {
            return ModelInfoRef?.mModelName ?? DrawArrayModelInfoRef?.mModelName ?? "";
            // if (ModelInfoRef != null) return ModelInfoRef.mModelName;
            // if (DrawArrayModelInfoRef != null) return DrawArrayModelInfoRef.mModelName;

            // return "";
        }


        public string GetModelFileName()
        {
            return ModelInfoRef?.mFilePath ?? DrawArrayModelInfoRef?.mFilePath ?? "";
            // if (ModelInfoRef != null) return ModelInfoRef.mFilePath;
            // if (DrawArrayModelInfoRef != null) return DrawArrayModelInfoRef.mFilePath;

            // return "";
        }

        private void Load(string path)
        {
            var stream = new MemoryStream(FileUtil.DecompressFile(path));
            SARC.SARC sarc = new SARC.SARC(stream);

            //Notes:
            //We load the component list rather than folders as there can be multiple components of the same type
            //Model info for example has multiple from model skin to use varied bfres skins
            foreach (var file in sarc.GetFiles("Actor"))
            {
                var paramInfo = BymlSerialize.Deserialize<ActorParam>(sarc.OpenFile(file));

                paramInfo.path = file;
                if(paramInfo.Components != null)
                    paramTree.Add(GetPathGyml(paramInfo.parent ?? "root"), paramInfo);
            }
            
            foreach (var param in paramTree.Where(x => !paramTree.ContainsKey(x.Value.path)))
            {     
                LoadComponents(sarc, param.Value);

                if (!string.IsNullOrEmpty(param.Value.Category))
                    this.Category = param.Value.Category;

                var parFile = param.Key;
                while(parFile != "root")
                {
                    var parent = paramTree.First(x => GetPathGyml(x.Value.path) == parFile);

                    LoadComponents(sarc, parent.Value);

                    if (!string.IsNullOrEmpty(parent.Value.Category))
                        this.Category = parent.Value.Category;

                    parFile = parent.Key;
                }
            }
            
            stream.Dispose();
        }

        private void LoadComponents(SARC.SARC sarc, ActorParam param)
        {
            if (param.Components == null)
                return;

            foreach (var component in param.Components)
            {
                string filePath = GetPathGyml((string)component.Value);
                var data = sarc.OpenFile(filePath);

                //Check if the component is present in the pack file.
                if (data == null)
                    continue;

                switch (component.Key)
                {
                    case "DrawArrayModelInfoRef":
                        if(this.DrawArrayModelInfoRef == null)
                            this.DrawArrayModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
                        else{
                            var par = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in this.DrawArrayModelInfoRef.GetType().GetProperties())
                            {
                                if (v.GetValue(this.DrawArrayModelInfoRef) == null || 
                                (v.PropertyType == Vector3.Zero.GetType() && (Vector3)(v.GetValue(this.DrawArrayModelInfoRef) ?? Vector3.Zero) == Vector3.Zero))
                                    v.SetValue(this.DrawArrayModelInfoRef, v.GetValue(par));
                            }
                        }

                        while (!string.IsNullOrEmpty(this.DrawArrayModelInfoRef.parent))
                        {
                            var file = GetPathGyml(this.DrawArrayModelInfoRef.parent);
                            data = sarc.OpenFile(file);
                            var par = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in this.DrawArrayModelInfoRef.GetType().GetProperties())
                            {
                                if (v.GetValue(this.DrawArrayModelInfoRef) == null || 
                                (v.PropertyType == Vector3.Zero.GetType() && (Vector3)(v.GetValue(this.DrawArrayModelInfoRef) ?? Vector3.Zero) == Vector3.Zero))
                                    v.SetValue(this.DrawArrayModelInfoRef, v.GetValue(par));
                            }
                            this.DrawArrayModelInfoRef.parent = par.parent;
                        }
                    break;
                    case "ModelInfoRef":
                        if(this.ModelInfoRef == null)
                            this.ModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
                        else{
                            var par = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in this.ModelInfoRef.GetType().GetProperties())
                            {
                                if (v.GetValue(this.ModelInfoRef) == null ||
                                (v.PropertyType == Vector3.Zero.GetType() && (Vector3)(v.GetValue(this.ModelInfoRef) ?? Vector3.Zero) == Vector3.Zero))
                                    v.SetValue(this.ModelInfoRef, v.GetValue(par));
                            }
                        }

                        while (!string.IsNullOrEmpty(this.ModelInfoRef.parent))
                        {
                            var file = GetPathGyml(ModelInfoRef.parent);
                            data = sarc.OpenFile(file);
                            var par = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in ModelInfoRef.GetType().GetProperties())
                            {
                                if (v.GetValue(this.ModelInfoRef) == null || 
                                (v.PropertyType == Vector3.Zero.GetType() && (Vector3)(v.GetValue(this.ModelInfoRef) ?? Vector3.Zero) == Vector3.Zero))
                                    v.SetValue(this.ModelInfoRef, v.GetValue(par));      
                            }
                            this.ModelInfoRef.parent = par.parent;
                        }
                    break;
                    case "ModelExpandRef":
                        this.ModelExpandParamRef ??= BymlSerialize.Deserialize<ModelExpandParam>(data);
                    break;
                    case "DrainPipeRef":
                        this.DrainPipeRef ??= BymlSerialize.Deserialize<DrainPipe>(data);
                    break;
                    case "GamePhysicsRef":
                        this.GamePhysicsRef = BymlSerialize.Deserialize<GamePhysics>(data);
                        if(!string.IsNullOrEmpty(this.GamePhysicsRef.mPath))
                            this.ShapeParams ??= GetActorShape(sarc);
                    break;
                    case "BgUnitInfo":
                        this.BgUnitInfo = BymlSerialize.Deserialize<BgUnitInfo>(data);
                    break;
                }
            }
        }

        private ShapeParamList? GetActorShape(SARC.SARC sarc)
        {
            var file = GetPathGyml(this.GamePhysicsRef.mPath);
            var dat = sarc.OpenFile(file);
            this.ControllerPath = BymlSerialize.Deserialize<ControllerSetParam>(dat);
            
            while (!string.IsNullOrEmpty(this.ControllerPath.parent) &&
            (this.ControllerPath.ShapeNamePathAry == null ||
            ((this.ControllerPath.mRigids?.Count ?? this.ControllerPath.mEntity?.Count ?? this.ControllerPath.mSensor?.Count ?? 0) == 0)))
            {
                file = GetPathGyml(this.ControllerPath.parent);
                dat = sarc.OpenFile(file);
                var par = BymlSerialize.Deserialize<ControllerSetParam>(dat);
                foreach(var v in ControllerPath.GetType().GetProperties())
                {
                    v.SetValue(this.ControllerPath, v.GetValue(this.ControllerPath) ?? v.GetValue(par));
                }
                this.ControllerPath.parent = par.parent;
            }
            
            var shapes = this.ControllerPath.ShapeNamePathAry ?? [];
            var rigidBodies = (this.ControllerPath.mRigids ?? [])
                .Concat((this.ControllerPath.mEntity ?? [])
                .Concat(this.ControllerPath.mSensor ?? []));

            foreach(var rigid in rigidBodies)
            {
                file = GetPathGyml(rigid.FilePath);
                dat = sarc.OpenFile(file);
                var body = BymlSerialize.Deserialize<RigidParam>(dat);

                while (!string.IsNullOrEmpty(body.parent) && 
                string.IsNullOrEmpty(body.ShapeName) && (body.ShapeNames?.Count ?? 0) == 0)
                {
                    file = GetPathGyml(body.parent);
                    dat = sarc.OpenFile(file);
                    body = BymlSerialize.Deserialize<RigidParam>(dat);
                }

                foreach(var shape in shapes)
                {
                    if(((body.ShapeName ?? "")  == shape.Name || (body.ShapeNames?.Cast<string>() ?? []).Contains(shape.Name)) && 
                    shape.FilePath != null)
                    {
                        file = GetPathGyml(shape.FilePath);
                        dat = sarc.OpenFile(file);
                        return BymlSerialize.Deserialize<ShapeParamList>(dat);
                    }
                }
            }
            return null;
        }

        private string GetPathGyml(string path)
        {
            string gyml = path.Replace("Work/", string.Empty);
            return gyml.Replace(".gyml", ".bgyml");
        }

        class ActorParam
        {
            public string path;

            [BymlProperty("$parent")]
            public string parent { get; set; }

            public string Category { get; set; }
            public Dictionary<string, object> Components { get; set; } 
        }
    }
}

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
            if (ModelInfoRef != null) return ModelInfoRef.mModelName;
            if (DrawArrayModelInfoRef != null) return DrawArrayModelInfoRef.mModelName;

            return "";
        }


        public string GetModelFileName()
        {
            if (ModelInfoRef != null) return ModelInfoRef.mFilePath;
            if (DrawArrayModelInfoRef != null) return DrawArrayModelInfoRef.mFilePath;

            return "";
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

            foreach (var param in paramTree)
            {     
                if(param.Key == "root")
                {
                    LoadComponents(sarc, param.Value);

                    if (!string.IsNullOrEmpty(param.Value.Category))
                        this.Category = param.Value.Category;

                    var parFile = param.Value.path;
                    while(paramTree.ContainsKey(parFile))
                    {
                        var parent = paramTree[parFile];

                        LoadComponents(sarc, parent);

                        if (!string.IsNullOrEmpty(parent.Category))
                            this.Category = parent.Category;

                        parFile = parent.path;
                    }
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
                        if(DrawArrayModelInfoRef == null)
                            this.DrawArrayModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
                        else{
                            var child = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in DrawArrayModelInfoRef.GetType().GetProperties())
                            {
                                if (v.GetValue(child) != null && v.GetValue(child) != default)
                                    v.SetValue(DrawArrayModelInfoRef, v.GetValue(child));
                            }
                        }
                        break;
                    case "ModelInfoRef":
                        if(ModelInfoRef == null)
                            this.ModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
                        else{
                            var child = BymlSerialize.Deserialize<ModelInfo>(data);
                            foreach(var v in ModelInfoRef.GetType().GetProperties())
                            {
                                
                                if (v.GetValue(child) != null && v.GetValue(child) != default)
                                    v.SetValue(ModelInfoRef, v.GetValue(child));
                            }
                        }
                        break;
                    case "ModelExpandRef":
                        this.ModelExpandParamRef = BymlSerialize.Deserialize<ModelExpandParam>(data);
                        break;
                    case "DrainPipeRef":
                        this.DrainPipeRef = BymlSerialize.Deserialize<DrainPipe>(data);
                        break;
                    case "GamePhysicsRef":
                        this.GamePhysicsRef = BymlSerialize.Deserialize<GamePhysics>(data);
                        if(!string.IsNullOrEmpty(GamePhysicsRef.mPath))
                            ShapeParams = GetActorShape(sarc, data, filePath);
                        break;
                }
            }
        }

        private ShapeParamList GetActorShape(SARC.SARC sarc, byte[] data, string filePath)
        {
            filePath = GetPathGyml(GamePhysicsRef.mPath);
            data = sarc.OpenFile(filePath);
            ControllerPath = BymlSerialize.Deserialize<ControllerSetParam>(data);

            while (!string.IsNullOrEmpty(ControllerPath.parent) &&
            (ControllerPath.ShapeNamePathAry == null ||
            (ControllerPath.mRigids == null && ControllerPath.mEntity == null)))
            {
                filePath = GetPathGyml(ControllerPath.parent);
                data = sarc.OpenFile(filePath);
                var par = BymlSerialize.Deserialize<ControllerSetParam>(data);
                foreach(var v in ControllerPath.GetType().GetProperties())
                {
                    if (v.GetValue(ControllerPath) == null && v.GetValue(par) != null)
                        v.SetValue(ControllerPath, v.GetValue(par));
                }
                ControllerPath.parent = par.parent;
            }
            
            if(ControllerPath.ShapeNamePathAry == null &&
            ControllerPath.mRigids == null && ControllerPath.mEntity == null)
            {
                var shapes = ControllerPath.ShapeNamePathAry;
                var rigidBodies = (ControllerPath.mRigids ?? new()).Concat(ControllerPath.mEntity ?? new());

                foreach(var rigid in rigidBodies)
                {
                    filePath = GetPathGyml(rigid.FilePath);
                    data = sarc.OpenFile(filePath);
                    var body = BymlSerialize.Deserialize<RigidParam>(data);

                    foreach(var shape in shapes)
                    {
                        if(body.ShapeName != null)
                        {
                            if(body.ShapeName == shape.Name && shape.FilePath != null)
                            {
                                filePath = GetPathGyml(shape.FilePath);
                                data = sarc.OpenFile(filePath);
                                return BymlSerialize.Deserialize<ShapeParamList>(data);
                            }
                        }
                        else if(body.ShapeNames != null)
                        {
                            if(body.ShapeNames.Cast<string>().Contains(shape.Name) && shape.FilePath != null)
                            {
                                filePath = GetPathGyml(shape.FilePath);
                                data = sarc.OpenFile(filePath);
                                return BymlSerialize.Deserialize<ShapeParamList>(data);
                            }
                        }
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

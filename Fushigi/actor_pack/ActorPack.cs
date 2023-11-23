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

                LoadComponents(sarc, paramInfo);

                if (!string.IsNullOrEmpty(paramInfo.Category))
                    this.Category = paramInfo.Category;
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
                        this.DrawArrayModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
                        break;
                    case "ModelInfoRef":
                        this.ModelInfoRef = BymlSerialize.Deserialize<ModelInfo>(data);
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
                            GetActorShape(sarc, data, filePath);

                        break;
                }
            }
        }

        private void GetActorShape(SARC.SARC sarc, byte[] data, string filePath)
        {
            filePath = GetPathGyml(GamePhysicsRef.mPath);
            data = sarc.OpenFile(filePath);
            ControllerPath = BymlSerialize.Deserialize<ControllerSetParam>(data);
            if(ControllerPath.ShapeNamePathAry != null)
            {
                var shapes = ControllerPath.ShapeNamePathAry;
                if (!string.IsNullOrEmpty(ControllerPath.parent))
                {
                    filePath = GetPathGyml(ControllerPath.parent);
                    data = sarc.OpenFile(filePath);
                    ControllerPath = BymlSerialize.Deserialize<ControllerSetParam>(data);
                }
                var rigidBodies = ControllerPath.mRigids != null && ControllerPath.mRigids.Count > 0 ? ControllerPath.mRigids : ControllerPath.mEntity;

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
                                ShapeParams = BymlSerialize.Deserialize<ShapeParamList>(data);
                            }
                        }
                        else if(body.ShapeNames != null)
                        {
                            if(body.ShapeNames.Cast<string>().Contains(shape.Name) && shape.FilePath != null)
                            {
                                filePath = GetPathGyml(shape.FilePath);
                                data = sarc.OpenFile(filePath);
                                ShapeParams = BymlSerialize.Deserialize<ShapeParamList>(data);
                            }
                        }
                    }
                }
            }
        }

        private string GetPathGyml(string path)
        {
            string gyml = path.Replace("Work/", string.Empty);
            return gyml.Replace(".gyml", ".bgyml");
        }

        class ActorParam
        {
            public string Category { get; set; }
            public Dictionary<string, object> Components { get; set; } 
        }
    }
}

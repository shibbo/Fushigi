using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Ryujinx.Common.Memory.MemoryStreamManager;
using System.Xml.Linq;
using Fushigi.Bfres.Common;
using Ryujinx.Common.Logging;
using System.Reflection.PortableExecutable;
using System.Numerics;
using Ryujinx.Common.Collections;
using System.Diagnostics;

namespace Fushigi.Bfres
{
    public class Skeleton : IResData
    {
        public ResDict<Bone> Bones { get; set; } = new ResDict<Bone>();

        public ushort[] MatrixToBoneList { get; set; }
        public ushort NumSmoothMatrices => header.NumSmoothMatrices;
        public ushort NumRigidMatrices => header.NumRigidMatrices;

        private SkeletonHeader header;

        public void Read(BinaryReader reader)
        {
            header = new SkeletonHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            long pos = reader.BaseStream.Position;

            var num_bone_indices = header.NumSmoothMatrices + header.NumRigidMatrices;

            Bones = reader.ReadDictionary<Bone>(header.BoneDictionaryOffset, header.BoneArrayOffset);
            MatrixToBoneList = reader.ReadCustom(() => reader.ReadUInt16s(num_bone_indices), header.MatrixToBoneListOffset);

            CalculateMatrices(true);

            //return
            reader.SeekBegin(pos);
        }

        public void RecalculateMatrices() => CalculateMatrices(false);

        private void CalculateMatrices(bool calculateInverse)
        {
            for (int i = 0; i < Bones.Count; i++)
            {
                var bone = Bones[i];
                var localMatrix = bone.CalculateLocalMatrix();

                bone.WorldMatrix = localMatrix;

                if (bone.ParentIndex != -1)
                {
                    Debug.Assert(bone.ParentIndex < i);
                    var parent = Bones[bone.ParentIndex];
                    bone.WorldMatrix *= parent.WorldMatrix;
                }

                if (calculateInverse)
                {
                    Matrix4x4.Invert(bone.WorldMatrix, out Matrix4x4 inv);
                    bone.InverseMatrix = inv;
                }
            }
        }
    }

    public class Bone : IResData
    {
        private const uint _flagsMask = 0b00000000_00000000_00000000_00000001;
        private const uint _flagsMaskRotate = 0b00000000_00000000_01110000_00000000;

        private uint _flags
        {
            get { return header.Flags; }
            set { header.Flags = value; }
        }

        public string Name { get; set; }

        public BoneFlagsRotation FlagsRotation
        {
            get { return (BoneFlagsRotation)(_flags & _flagsMaskRotate); }
            set { _flags = _flags & ~_flagsMaskRotate | (uint)value; }
        }

        public Vector3 Position
        {
            get { return new Vector3(header.PositionX, header.PositionY, header.PositionZ); }
        }

        public Vector4 Rotate
        {
            get { return new Vector4(header.RotationX, header.RotationY, 
                                     header.RotationZ, header.RotationW);
            }
        }

        public Vector3 Scale
        {
            get { return new Vector3(header.ScaleX, header.ScaleY, header.ScaleZ); }
        }

        public int Index => header.Index;

        public short SmoothMatrixIndex => header.SmoothMatrixIndex;
        public short RigidMatrixIndex => header.RigidMatrixIndex;
        public short ParentIndex => header.ParentIndex;

        public Matrix4x4 WorldMatrix;
        public Matrix4x4 InverseMatrix;

        private BoneHeader header;

        public void Read(BinaryReader reader)
        {
            header = new BoneHeader();
            reader.BaseStream.Read(Utils.AsSpan(ref header));

            Name = reader.ReadStringOffset(header.NameOffset);
        }

        public Matrix4x4 CalculateLocalMatrix()
        {
            Matrix4x4 transMatrix = Matrix4x4.CreateTranslation(this.Position);
            Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(this.Scale);
            Matrix4x4 rotMatrix = Matrix4x4.Identity;

            if (this.FlagsRotation == BoneFlagsRotation.Quaternion)
            {
                rotMatrix = Matrix4x4.CreateFromQuaternion(new Quaternion(
                    this.Rotate.X, this.Rotate.Y, this.Rotate.Z, this.Rotate.W));
            }
            else
            {
                rotMatrix = Matrix4x4.CreateRotationX(this.Rotate.X) *
                            Matrix4x4.CreateRotationY(this.Rotate.Y) *
                            Matrix4x4.CreateRotationZ(this.Rotate.Z);
            }
            return scaleMatrix * rotMatrix * transMatrix;
        }
        public enum BoneFlagsRotation : uint
        {
            Quaternion,
            EulerXYZ = 1 << 12
        }
    }
}

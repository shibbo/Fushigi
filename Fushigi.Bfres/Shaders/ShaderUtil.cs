using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Fushigi.Bfres.BfshaFile;

namespace Fushigi.Bfres.Shaders
{
    public class ShaderUtil
    {
        /// <summary>
        /// A helper method to generate shader param data for rendering.
        /// </summary>
        public static byte[] GenerateShaderParamBuffer(ShaderModel shaderModel, Material material)
        {
            //Fill the buffer by program offsets
            var mem = new System.IO.MemoryStream();
            using (var writer = new BinaryWriter(mem))
            {
                var matBlock = shaderModel.UniformBlocks.Values.FirstOrDefault(x => x.Type == 1);

                int index = 0;
                foreach (var param in matBlock.Uniforms.Values)
                {
                    var uniformName = matBlock.Uniforms.GetKey(index++);

                    writer.Seek(param.DataOffset - 1, SeekOrigin.Begin);
                    if (material.ShaderParams.ContainsKey(uniformName))
                    {
                        var matParam = material.ShaderParams[uniformName];

                        if (matParam.Type == ShaderParam.ShaderParamType.TexSrtEx) //Texture matrix (texmtx)
                            writer.Write(CalculateSRT3x4((ShaderParam.TexSrt)matParam.DataValue));
                        else if (matParam.Type == ShaderParam.ShaderParamType.TexSrt)
                            writer.Write(CalculateSRT2x3((ShaderParam.TexSrt)matParam.DataValue));
                        else if (matParam.DataValue is ShaderParam.Srt2D) //Indirect SRT (ind_texmtx)
                            writer.Write(CalculateSRT((ShaderParam.Srt2D)matParam.DataValue));
                        else if (matParam.DataValue is float)
                            writer.Write((float)matParam.DataValue);
                        else if (matParam.DataValue is float[])
                            writer.Write((float[])matParam.DataValue);
                        else if (matParam.DataValue is int[])
                            writer.Write((int[])matParam.DataValue);
                        else if (matParam.DataValue is uint[])
                            writer.Write((uint[])matParam.DataValue);
                        else if (matParam.DataValue is int)
                            writer.Write((int)matParam.DataValue);
                        else if (matParam.DataValue is uint)
                            writer.Write((uint)matParam.DataValue);
                        else if (matParam.DataValue is bool)
                            writer.Write((bool)matParam.DataValue);
                        else
                            throw new Exception($"Unsupported render type! {matParam.Type}");
                    }
                }
                writer.AlignBytes(16);
            }
            return mem.ToArray();
        }

        public static float[] CalculateSRT3x4(ShaderParam.TexSrt texSrt)
        {
            var m = CalculateSRT2x3(texSrt);
            return new float[12]
            {
                m[0], m[2], m[4], 0.0f,
                m[1], m[3], m[5], 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
            };
        }

        public static float[] CalculateSRT2x3(ShaderParam.TexSrt texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);
            float scalingXC = scaling.X * cosR;
            float scalingXS = scaling.X * sinR;
            float scalingYC = scaling.Y * cosR;
            float scalingYS = scaling.Y * sinR;

            switch (texSrt.Mode)
            {
                default:
                case ShaderParam.TexSrt.TexSrtMode.ModeMaya:
                    return new float[8]
                    {
                        scalingXC, -scalingYS,
                        scalingXS, scalingYC,
                        -0.5f * (scalingXC + scalingXS - scaling.X) - scaling.X * translate.X, -0.5f * (scalingYC - scalingYS + scaling.Y) + scaling.Y * translate.Y + 1.0f,
                        0.0f, 0.0f,
                    };
                case ShaderParam.TexSrt.TexSrtMode.Mode3dsMax:
                    return new float[8]
                    {
                        scalingXC, -scalingYS,
                        scalingXS, scalingYC,
                        -scalingXC * (translate.X + 0.5f) + scalingXS * (translate.Y - 0.5f) + 0.5f, scalingYS * (translate.X + 0.5f) + scalingYC * (translate.Y - 0.5f) + 0.5f,
                        0.0f, 0.0f
                    };
                case ShaderParam.TexSrt.TexSrtMode.ModeSoftimage:
                    return new float[8]
                    {
                        scalingXC, scalingYS,
                        -scalingXS, scalingYC,
                        scalingXS - scalingXC * translate.X - scalingXS * translate.Y, -scalingYC - scalingYS * translate.X + scalingYC * translate.Y + 1.0f,
                        0.0f, 0.0f,
                    };
            }
        }

        public static float[] CalculateSRT(ShaderParam.Srt2D texSrt)
        {
            var scaling = texSrt.Scaling;
            var translate = texSrt.Translation;
            float cosR = (float)Math.Cos(texSrt.Rotation);
            float sinR = (float)Math.Sin(texSrt.Rotation);

            return new float[8]
            {
                scaling.X * cosR, scaling.X * sinR,
                -scaling.Y * sinR, scaling.Y * cosR,
                translate.X, translate.Y,
                0.0f, 0.0f
            };
        }
    }
}

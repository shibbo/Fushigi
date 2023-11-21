using Fushigi.Bfres;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;

namespace Fushigi.gl.Bfres
{
    public class TegraShaderDecoder
    {
        private static Dictionary<string, GLShader> shader_cache = new Dictionary<string, GLShader>();

        public static ShaderInfo LoadShaderProgram(GL gl, BnshFile.ShaderVariation variation)
        {
            var shaderData = variation.BinaryProgram;

            var vertexData = shaderData.VertexShader.ByteCode;
            var fragData = shaderData.FragmentShader.ByteCode;
            var controlV = shaderData.VertexShader.ControlCode;
            var controlF = shaderData.FragmentShader.ControlCode;

            return LoadShaderProgram(gl, controlV, controlF, vertexData, fragData);
        }

        public static ShaderInfo LoadShaderProgram(GL gl, Span<byte> vertexControlShader,
               Span<byte> fragControlShader, Span<byte> vertexShader,  Span<byte> fragShader)
        { 
            //Folder to store shader caches
            string cacheFolder = Path.Combine("ShaderCache", "OpenGL");
            //Create if not present
            if (!Directory.Exists(cacheFolder))
                Directory.CreateDirectory(cacheFolder);

            //Cached file path
            string fragHash = GetHashSHA1(fragShader);
            string vertHash = GetHashSHA1(vertexShader);

            string vertPath = Path.Combine(cacheFolder, $"{vertHash}.vert");
            string fragPath = Path.Combine(cacheFolder, $"{fragHash}.frag");

            //Find shader constants
            var vertexConstants = GetConstants(vertexControlShader, vertexShader);
            var fragConstants = GetConstants(fragControlShader, fragShader);

            //Combine each shader into a key for finding already loaded shaders
            string key = $"{vertHash}_{fragHash}";

            if (shader_cache.ContainsKey(key))
                return new ShaderInfo()
                {
                    Shader = shader_cache[key],
                    VertexConstants = vertexConstants.ToArray(),
                    FragmentConstants = fragConstants.ToArray(),
                };

            //Save each shader into the cache if not present and decompile them
            if (!File.Exists(vertPath))
            {
                File.WriteAllText(vertPath,
                      DecompileShader(vertexShader));
            }
            if (!File.Exists(fragPath))
                File.WriteAllText(fragPath,
                     DecompileShader(fragShader));

            //Load the source to opengl
            var program = GLShader.FromFilePath(gl, vertPath, fragPath);
            //Cache for reuse
            shader_cache.Add(key, program);

            return new ShaderInfo()
            {
                Shader = program,
                VertexConstants = vertexConstants.ToArray(),
                FragmentConstants = fragConstants.ToArray(),
            };
        }

        static string DecompileShader(Span<byte> Data)
        {
            return TegraShaderTranslator.TranslateShader(Data.Slice(48, Data.Length - 48).ToArray());
        }

        public static Span<byte> GetConstants(Span<byte> control, Span<byte> bytecode)
        {
            if (control == null) return new byte[0];

            //Bnsh has 2 shader code sections. The first section has block info for constants
            using (var reader = new BinaryReader(new MemoryStream(control.ToArray())))
            {
                reader.BaseStream.Seek(1776, SeekOrigin.Begin);
                ulong ofsUnk = reader.ReadUInt64();
                uint lenByteCode = reader.ReadUInt32();
                uint lenConstData = reader.ReadUInt32();
                uint ofsConstBlockDataStart = reader.ReadUInt32();
                uint ofsConstBlockDataEnd = reader.ReadUInt32();

                return bytecode.Slice((int)ofsConstBlockDataStart, (int)lenConstData);
            }
        }

        //Hash algorithm for cached shaders. Make sure to only decompile unique/new shaders
        static string GetHashSHA1(Span<byte> data)
        {
            using (var sha1 = new System.Security.Cryptography.SHA1CryptoServiceProvider()) {
                return string.Concat(sha1.ComputeHash(data.ToArray()).Select(x => x.ToString("X2")));
            }
        }

        public class ShaderInfo
        {
            public GLShader Shader;

            public byte[] VertexConstants;
            public byte[] FragmentConstants;
        }
    }
}

using Ryujinx.Graphics.Shader.Translation;
using Ryujinx.Graphics.Shader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fushigi.course;
using Silk.NET.OpenGL;
using System.Runtime.InteropServices;

namespace Fushigi.gl.Bfres
{
    public class TegraShaderTranslator
    {
        public static string TranslateShader(byte[] data)
        {
            TranslationFlags flags = TranslationFlags.DebugMode;

            TranslationOptions translationOptions = new TranslationOptions(TargetLanguage.Glsl, TargetApi.OpenGL, flags);
            ShaderProgram program = Translator.CreateContext(0, new GpuAccessor(data), translationOptions).Translate();
            return program.Code;
        }

        private class GpuAccessor : IGpuAccessor
        {
            private readonly byte[] _data;

            public GpuAccessor(byte[] data)
            {
                _data = data;
            }

            public ReadOnlySpan<ulong> GetCode(ulong address, int minimumSize)
            {
                return MemoryMarshal.Cast<byte, ulong>(new ReadOnlySpan<byte>(_data).Slice((int)address));
            }
        }
    }
}

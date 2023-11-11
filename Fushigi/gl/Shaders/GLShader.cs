using System;
using System.IO;
using System.Numerics;
using Silk.NET.OpenGL;

namespace Fushigi.gl
{
    public class GLShader : GLObject, IDisposable
    {
        private GL _gl;

        public GLShader(GL gl) : base(gl.CreateProgram())
        {
            _gl = gl;
        }

        public static GLShader FromFilePath(GL gl, string vertexPath, string fragmentPath)
        {
            GLShader shader = new GLShader(gl);
            shader.Init(gl, File.ReadAllText(vertexPath), File.ReadAllText(fragmentPath));
            return shader;
        }

        public static GLShader FromSource(GL gl, string vertexCode, string fragmentCode)
        {
            GLShader shader = new GLShader(gl);
            shader.Init(gl, vertexCode, fragmentCode);
            return shader;
        }

        private void Init(GL gl, string vertexCode, string fragmentCode)
        {
            _gl = gl;

            uint vertex = LoadShader(ShaderType.VertexShader, vertexCode);
            uint fragment = LoadShader(ShaderType.FragmentShader, fragmentCode);
            _gl.AttachShader(ID, vertex);
            _gl.AttachShader(ID, fragment);
            _gl.LinkProgram(ID);
            _gl.GetProgram(ID, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(ID)}");
            }
            _gl.DetachShader(ID, vertex);
            _gl.DetachShader(ID, fragment);
            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
        }

        public void Use()
        {
            _gl.UseProgram(ID);
        }

        public int GetAttribute(string name)
        {
            return _gl.GetAttribLocation(ID, name);
        }

        public void SetTexture(string uniform, GLTexture texture, int slot)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + slot);
            texture.Bind();
            SetUniform(uniform, 1);
        }

        public void SetUniform(string name, int value)
        {
            int location = _gl.GetUniformLocation(ID, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = _gl.GetUniformLocation(ID, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.UniformMatrix4(location, 1, false, (float*)&value);
        }

        public void SetUniform(string name, float value)
        {
            int location = _gl.GetUniformLocation(ID, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }

        public void SetUniform(string name, Vector3 value)
        {
            int location = _gl.GetUniformLocation(ID, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform3(location, value.X, value.Y, value.Z);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(ID);
        }

        private uint LoadShader(ShaderType type, string src)
        {
            uint handle = _gl.CreateShader(type);
            _gl.ShaderSource(handle, src);
            _gl.CompileShader(handle);
            string infoLog = _gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
    }
}
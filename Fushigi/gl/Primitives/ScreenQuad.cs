using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Fushigi.gl;
using Fushigi.gl.Shaders;
using Fushigi.ui.widgets;
using Microsoft.VisualBasic;
using Silk.NET.OpenGL;
using Silk.NET.SDL;

namespace Fushigi.gl
{
    public class ScreenQuad : RenderMesh<VertexPositionTexCoord>
    {
        public ScreenQuad(GL gl, float size, bool flipY = false) : base(gl, GetVertices(size, flipY), null, PrimitiveType.TriangleStrip)
        {

        }

        static VertexPositionTexCoord[] GetVertices(float size, bool flipY = false)
        {
            VertexPositionTexCoord[] vertices = new VertexPositionTexCoord[4];
            for (int i = 0; i < 4; i++)
            {
                vertices[i] = new VertexPositionTexCoord()
                {
                    Position = new Vector3(positions[i].X * size, positions[i].Y * size, 0),
                    TexCoord = flipY ? new Vector2(texCoords[i].X, 1.0f - texCoords[i].Y) : texCoords[i],
                };
            }
            return vertices;
        }

        static Vector2[] positions =
        [
                    new Vector2(-1.0f, 1.0f),
                    new Vector2(-1.0f, -1.0f),
                    new Vector2(1.0f, 1.0f),
                    new Vector2(1.0f, -1.0f),
        ];

        static Vector2[] texCoords =
        {
                    new Vector2(0.0f, 0.0f),
                    new Vector2(0.0f, 1.0f),
                    new Vector2(1.0f, 0.0f),
                    new Vector2(1.0f, 1.0f),
        };
    }
}

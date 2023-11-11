#version 330

in vec3 aPosition;
in vec3 aNormal;

in vec2 aTexCoord0;
in vec2 aTexCoord1;

in vec4 aTangent;

uniform mat4 mtxCam;
uniform mat4 mtxMdl;

out vec2 TexCoords0;
out vec3 Normals;
out vec4 Tangents;

void main()
{
    gl_Position = mtxCam * (mtxMdl * vec4(aPosition, 1.0)); 
    TexCoords0 = aTexCoord0;
    Normals = aNormal;
    Tangents = aTangent;
}
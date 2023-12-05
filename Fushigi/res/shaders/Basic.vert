#version 330

layout (location = 0) in vec3 aPos;
layout (location = 1) in vec2 aTexCoord0;

uniform mat4 mtxCam;

out vec2 TexCoords;

void main()
{
    gl_Position = mtxCam * vec4(aPos, 1.0); 
    TexCoords = aTexCoord0;
}
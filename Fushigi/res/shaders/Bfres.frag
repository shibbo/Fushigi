#version 330

in vec2 TexCoords0;
in vec3 Normals;
in vec4 Tangents;


uniform int hasTexture;
uniform sampler2D image;

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0);
    if (hasTexture == 1)
        FragColor.rgb = texture(image, TexCoords0).rgb;

    vec3 displayNormal = (Normals * 0.5) + 0.5;
    FragColor.rgb = displayNormal;
}
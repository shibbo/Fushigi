#version 330

in vec2 TexCoords;

uniform int hasTexture;
uniform sampler2D image;

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0);
    if (hasTexture == 1)
        FragColor.rgb = texture(image, TexCoords).rgb;
}
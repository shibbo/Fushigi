#version 330

in vec2 TexCoords;

uniform sampler2D screenTexture;

out vec4 FragColor;

const float GAMMA = 2.2;

void main()
{
    FragColor.rgb = texture(screenTexture, TexCoords).rgb;
    FragColor.a = 1.0;

    FragColor.rgb = pow(FragColor.rgb, vec3(1.0/GAMMA));
}
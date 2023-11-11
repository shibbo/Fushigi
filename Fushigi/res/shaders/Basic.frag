#version 330

in vec2 TexCoords;

uniform int use_albedo_map;
uniform int use_normal_map;
uniform int use_mix_map;

uniform sampler2D albedo_map;
uniform sampler2D normal_map;
uniform sampler2D mix_map;

out vec4 FragColor;

void main()
{
    FragColor = vec4(1.0);
    if (use_albedo_map == 1)
        FragColor.rgb = texture(albedo_map, TexCoords).rgb;
}
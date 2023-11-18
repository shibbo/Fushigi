#version 330

in vec2 TexCoords0;
in vec3 Normals;
in vec4 Tangents;

uniform int hasAlbedoMap;
uniform int hasNormalMap;
uniform int hasEmissiveMap;
uniform int hasAlphaMaskMap;
uniform int hasMixMap;

uniform vec4 const_color0;

uniform sampler2D albedo_texture;
uniform sampler2D normal_texture;
uniform sampler2D alpha_mask_texture;
uniform sampler2D mix_texture;

//Array types for tiles
uniform sampler2DArray albedo_array_texture;
uniform sampler2DArray normal_array_texture;
uniform sampler2DArray alpha_mask_array_texture;
uniform sampler2DArray mix_array_texture;

uniform int is_terrain_tile;
uniform int tile_id;

uniform int expression;

uniform mat4 mtxCam;
uniform vec3 difLightDirection;

out vec4 FragColor;

float Luminance(vec3 rgb)
{
    const vec3 W = vec3(0.2125, 0.7154, 0.0721);
    return dot(rgb, W);
}

float saturate(float v)
{
    return clamp(v, 0.0, 1.0);
}

vec3 ReconstructNormal(vec2 t_NormalXY) //computes Z, from noclip
{
   float t_NormalZ = sqrt(saturate(1.0 - dot(t_NormalXY.xy, t_NormalXY.xy)));
   return vec3(t_NormalXY.xy, t_NormalZ);
}

vec3 CalcNormalMap(vec3 inputNormal, vec2 normalMapXY)
{
    // Calculate the resulting normal map and intensity.
	vec3 normalMap = ReconstructNormal(normalMapXY);

    // TBN Matrix.
    vec3 T = Tangents.xyz;
    vec3 B = vec3(0); //game doesn't use bitangents

    if (Luminance(B) < 0.01)
        B = normalize(cross(T,  inputNormal));
    mat3 tbnMatrix = mat3(T, B,  inputNormal);

    vec3 newNormal = tbnMatrix * normalMap;
    return normalize(newNormal);
}

vec4 CalculateAlbedo(vec4 tex)
{
    return tex;
}

void main()
{
    FragColor = vec4(1.0);

    vec4 albedo = vec4(1.0);
    vec3 normals = Normals;

    //Sampler data
    if (is_terrain_tile == 1)
    {
        vec3 coords = vec3(TexCoords0, tile_id);
        if (hasAlbedoMap == 1)
            albedo = CalculateAlbedo(texture(albedo_array_texture, coords).rgba);
    }
    else
    {
        if (hasAlbedoMap == 1)
            albedo = CalculateAlbedo(texture(albedo_texture, TexCoords0).rgba);
        if (hasNormalMap == 1)
        {
           vec4 normal_map = texture(normal_texture, TexCoords0).rgba;
           normals = CalcNormalMap(Normals, normal_map.xy);
        }
    }

    float halfLambert = dot(difLightDirection, normals) * 0.5 + 0.5;

    if (hasAlbedoMap == 1)
        FragColor.rgba = albedo.rgba;

    FragColor.rgb *= const_color0.rgb;
    FragColor.rgb *= halfLambert * 2;
}
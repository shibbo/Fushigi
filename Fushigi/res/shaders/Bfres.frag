#version 330

in vec2 TexCoords0;
in vec3 Normals;
in vec4 Tangents;

uniform int hasAlbedoMap;
uniform int hasAlbedoMapArray;

uniform int hasNormalMap;
uniform int hasNormalMapArray;

uniform int hasAlphaMaskMap;
uniform int hasAlphaMaskMapArray;

uniform int hasMixMap;
uniform int hasMixMapArray;

uniform int hasEmissiveMap;

uniform vec4 const_color0;

uniform sampler2D albedo_texture;
uniform sampler2D normal_texture;
uniform sampler2D alpha_mask_texture;
uniform sampler2D mix_texture;

//Array types for tiles
uniform sampler2DArray albedo_texture_array;
uniform sampler2DArray normal_texture_array;
uniform sampler2DArray alpha_mask_texture_array;
uniform sampler2DArray mix_texture_array;

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
    vec3 coords_tile = vec3(TexCoords0, tile_id);

    //Sampler data
    if (hasAlbedoMap == 1)
        albedo = CalculateAlbedo(texture(albedo_texture, TexCoords0).rgba);
    else if (hasAlbedoMapArray == 1)
        albedo = CalculateAlbedo(texture(albedo_texture_array, coords_tile).rgba);

    if (hasNormalMap == 1)
        normals = CalcNormalMap(Normals, texture(normal_texture, TexCoords0).rg);
    else if (hasNormalMapArray == 1)
        normals = CalcNormalMap(Normals, texture(normal_texture_array, coords_tile).rg);

    float halfLambert = dot(difLightDirection, normals) * 0.5 + 0.5;

    if (hasAlbedoMap == 1)
        FragColor.rgba = albedo.rgba;

    FragColor.rgb *= const_color0.rgb;
    FragColor.rgb *= halfLambert * 2;
}
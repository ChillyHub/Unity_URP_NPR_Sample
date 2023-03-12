#ifndef PBR_SKYBOX_LUT_PASS_INCLUDED
#define PBR_SKYBOX_LUT_PASS_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 viewPosWS : TEXCOORD1;
    float3 viewDir : TEXCOORD2;
    float3 color : TEXCOORD3;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

TEXTURE2D(_SkyboxLUT);
SAMPLER(sampler_SkyboxLUT);

CBUFFER_START(UnityPerMaterial)
    // Color
    float3 _LightColor;
    float3 _SunColor;
    float3 _DayBlueColor;
    float3 _DayGreenColor;
    float3 _DayRedColor;
    float3 _NightColor;
    float _Exposure;
    
    // Geometry
    float _Thickness;
    float _Radius;
    float _RScale;
    
    // Physic
    float _rayleighFac;
    float _mieFac;
    float _gMie;
    float _gSun;
    
    // Performance
    float _SampleCounts;
CBUFFER_END

#define RADIUS (_Radius * 1000.0)        // km to m
#define THICKNESS (_Thickness * 1000.0)  // km to m

#include "Assets/Shaders/ShaderLibrary/AtmosphereScattering.hlsl"

Varyings PBRSkyboxPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

    Light light = GetMainLight();
    output.viewPosWS = GetCameraPositionWS();
    output.viewDir = normalize(output.viewPosWS - output.positionWS);

    ScatteringData data = GetScatteringData(
        _LightColor, RADIUS, THICKNESS, _RScale, _Exposure, _gMie, _rayleighFac, _mieFac, _SampleCounts);

    float3 inscatter = GetInscatteringColorLUT(
        _SkyboxLUT, sampler_SkyboxLUT, data, light.direction, output.viewDir, output.viewPosWS);
    float3 color = _DayRedColor * inscatter.r + _DayGreenColor * inscatter.g + _DayBlueColor * inscatter.b;
    
    float inscatterFac = smoothstep(0.00, 0.1, length(normalize(inscatter)));
    output.color = lerp(_NightColor, color, inscatterFac);

    return output;
}

float4 PBRSkyboxPassFragment(Varyings input) : SV_Target
{
    Light light = GetMainLight();
    float3 viewDir = normalize(input.viewPosWS - input.positionWS);
    return float4(input.color + GetSunRenderColor(_SunColor, light.direction, viewDir, _gSun), 1.0);
}

#endif
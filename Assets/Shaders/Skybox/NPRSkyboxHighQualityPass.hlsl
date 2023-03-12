#ifndef NPR_SKYBOX_HIGH_QUALITY_INCLUDED
#define NPR_SKYBOX_HIGH_QUALITY_INCLUDED

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

CBUFFER_START(UnityPerMaterial)
    float3 _ColorLight;
    float3 _ColorDayBright;
    float3 _ColorDayMiddle;
    float3 _ColorDayDark;
    float3 _ColorNight;
    float3 _ColorSun;
    float3 _ColorMoon;
    float _Exposure;

    float _Thickness;
    float _Radius;
    float _RScale;

    float _rayleighFac;
    float _mieFac;
    float _gMie;
    float _gSun;

    float _SampleCounts;
CBUFFER_END

#define RADIUS (_Radius * 1000.0)        // km to m
#define THICKNESS (_Thickness * 1000.0)  // km to m

#include "Assets/Shaders/ShaderLibrary/AtmosphereScattering.hlsl"

Varyings NPRSkyboxHighQualityPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

    Light light = GetMainLight();

    output.viewPosWS = GetCameraPositionWS();
    output.viewDir = normalize(output.viewPosWS - output.positionWS);

    ScatteringData data = GetScatteringData(_ColorLight, RADIUS, THICKNESS, _RScale, _Exposure, _gMie,
        _rayleighFac, _mieFac, _SampleCounts);

    float3 inscatter = GetInscatteringColor(data, light.direction, output.viewDir, output.viewPosWS);

    float brightness = inscatter.r + inscatter.g + inscatter.b;
    float3 color = _ColorDayDark * inscatter.r + _ColorDayMiddle * inscatter.g + _ColorDayBright * inscatter.b;
    color *= rcp(max(FLT_EPS, brightness)) * _Exposure;
    
    float inscatterFac = smoothstep(0.0, 0.1, brightness);
    output.color = lerp(_ColorNight, color, inscatterFac);

    return output;
}

float4 NPRSkyboxHighQualityPassFragment(Varyings input) : SV_Target
{
    Light light = GetMainLight();
    float3 viewDir = normalize(input.viewPosWS - input.positionWS);
    return float4(input.color + GetSunRenderColor(_ColorSun, light.direction, viewDir, _gSun), 1.0);
}

#endif

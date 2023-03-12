#ifndef NPR_SKYBOX_LOW_QUALITY_INCLUDED
#define NPR_SKYBOX_LOW_QUALITY_INCLUDED

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
    float3 color : TEXCOORD2;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityPerMaterial)
    float3 _ColorDayBright;
    float3 _ColorDayMiddle;
    float3 _ColorDayDark;
    float3 _ColorNight;
    float3 _ColorSun;
    float3 _ColorMoon;
    float _DayBrightRange;
    float _DayMiddleRange;
    float _DayDarkRange;
    float _Exposure;
    
    float _gMie;
    float _mieFac;
    float _gSun;
CBUFFER_END

#include "Assets/Shaders/ShaderLibrary/AtmosphereScattering.hlsl"

float3 GetSkyboxColor(float3 viewDir, float3 lightDir)
{
    float cos = dot(-viewDir, float3(0.0, 1.0, 0.0));
    float len = lerp(20.0, 1.0, pow(abs(cos), 0.2));

    float3 range = float3(_DayDarkRange, _DayMiddleRange, _DayBrightRange);
    float d = dot(lightDir, float3(0.0, 1.0, 0.0));
    float cTheta = dot(viewDir, lightDir);

    float3 fac = range * len * exp(-range * exp(-d) * len) * GetMiePhaseFunction(cTheta, _gMie);

    float3 color = (_ColorDayDark * fac.x + _ColorDayMiddle * fac.y + _ColorDayBright * fac.z) *
        rcp(fac.x + fac.y + fac.z);

    color = lerp(_ColorNight, color * _Exposure, saturate(d));
    
    return color;
}

Varyings NPRSkyboxLowQualityPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.viewPosWS = GetCameraPositionWS();

    Light light = GetMainLight();
    float3 viewDir = normalize(output.viewPosWS - output.positionWS);

    output.color = GetSkyboxColor(viewDir, light.direction);

    return output;
}

float4 NPRSkyboxLowQualityPassFragment(Varyings input) : SV_Target
{
    Light light = GetMainLight();
    float3 viewDir = normalize(input.viewPosWS - input.positionWS);
    return float4(input.color + GetSunRenderColor(_ColorSun, light.direction, viewDir, _gSun), 1.0);
}

#endif

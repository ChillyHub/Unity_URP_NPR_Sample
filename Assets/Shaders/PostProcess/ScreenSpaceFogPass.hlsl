#ifndef SCREEN_SPACE_RIM_PASS_INCLUDED
#define SCREEN_SPACE_RIM_PASS_INCLUDED

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

struct Attributes
{
    float4 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
    float3 viewRay : VAR_VIEW_RAY;
};

CBUFFER_START(UnityPerMaterial)
    float3 _LightColor;
    float _Exposure;
    float _Thickness;
    float _Radius;
    float _RScale;
    float _rayleighFac;
    float _mieFac;
    float _gMie;
    float _SampleCounts;
CBUFFER_END

#define RADIUS (_Radius * 1000.0)        // km to m
#define THICKNESS (_Thickness * 1000.0)  // km to m

#include "Assets/Shaders/ShaderLibrary/AtmosphereScattering.hlsl"

Varyings ScreenSpaceFogPassVertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.screenUV = input.baseUV;

    float depth = 1.0;
#if defined(UNITY_REVERSED_Z)
    depth = 0.0;
#endif

    // In order to recreate world position from depth map
    float3 positionWS = ComputeWorldSpacePosition(input.baseUV, depth, UNITY_MATRIX_I_VP);
    output.viewRay = positionWS - _WorldSpaceCameraPos.xyz;

    return output;
}

float4 ScreenSpaceFogPassFragment(Varyings input) : SV_Target
{
    float4 rgba = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.screenUV);
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.screenUV);
    depth = Linear01Depth(depth, _ZBufferParams);

    UNITY_BRANCH
    if (depth > 0.999)
    {
        return rgba;
    }

    Light light = GetMainLight();
    float3 color = rgba.rgb;
    float3 viewPos = _WorldSpaceCameraPos.xyz;
    float3 viewDir = -normalize(input.viewRay);
    float3 positionWS = viewPos + depth * input.viewRay;

    //const float3 sWaveLengths = float3(680.0, 550.0, 440.0) * 1e-9;
    //float len = length(positionWS - viewPos);
    //float cosTheta =  dot(viewDir, -light.direction);
    //float phase = GetTotalPhaseFunction(cosTheta, 0.0, 0.0);
    //float3 scattering = GetScatteringCoefficients(sWaveLengths);
//
    //float3 excintion = exp(-scattering * len);
//
    //float3 inscatter = GetInscatteringColorSimple(light.color, scattering, phase, len);

    ScatteringData data = GetScatteringData(
        _LightColor, RADIUS, THICKNESS, _RScale, _Exposure, _gMie, _rayleighFac, _mieFac, _SampleCounts);
    
    //CalculateAerialPerspectiveSimple(color, light.color, positionWS, viewPos, viewDir, light.direction);
    CalculateAerialPerspective(color, data, light.direction, viewDir, viewPos, positionWS);
    
    return float4(color, rgba.a);
}

#endif
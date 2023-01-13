#ifndef SCREEN_SPACE_RIM_PASS_INCLUDED
#define SCREEN_SPACE_RIM_PASS_INCLUDED

#include "../Utility.hlsl"

TEXTURE2D(_CameraOpaqueTexture);
SAMPLER(sampler_CameraOpaqueTexture);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

CBUFFER_START(UnityPerMaterial)
    float _Rim_Intensity;
    float _Rim_Bias;
    float _Rim_Threshold;
CBUFFER_END

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings ScreenSpaceRimPassVertex(uint vertexID : SV_VertexID)
{
    Varyings output;
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    
#if defined(UNITY_UV_STARTS_AT_TOP)
    output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? -1.0 : 1.0);
#else
    output.screenUV = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
#endif

    return output;
}

float4 ScreenSpaceRimPassFragment(Varyings input) : SV_Target
{
    float3 lightDirWS = GetMainLight().direction;
    float3 lightDirCS = TransformWorldToHClip(lightDirWS);
    float2 bias = normalize(lightDirCS.xy) * _Rim_Bias * _ZBufferParams.a;

    float2 trueUV = input.screenUV;
    float2 biasUV = trueUV + bias;

    float4 color = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, trueUV);
    float depthTrue = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, trueUV);
    float depthBias = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, biasUV);
    // float linearDepthTrue = Linear01Depth(depthTrue, _ZBufferParams);
    // float linearDepthBias = Linear01Depth(depthBias, _ZBufferParams);
    float linearDepthTrue = LinearEyeDepth(depthTrue, _ZBufferParams);
    float linearDepthBias = LinearEyeDepth(depthBias, _ZBufferParams);

    float isEdge = step(_Rim_Threshold, linearDepthBias - linearDepthTrue);
    float4 rim = _Rim_Intensity * color * isEdge;
    rim.a = 0.0;

    return color + rim;
}

#endif
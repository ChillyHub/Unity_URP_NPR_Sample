#ifndef AVATAR_INPUT_INCLUDED
#define AVATAR_INPUT_INCLUDED

TEXTURE2D(_DiffuseMap);
SAMPLER(sampler_DiffuseMap);
TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);
TEXTURE2D(_FaceLightMap);
SAMPLER(sampler_FaceLightMap);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_RampMap);
SAMPLER(sampler_RampMap);
TEXTURE2D(_MetalMap);
SAMPLER(sampler_MetalMap);

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

CBUFFER_START(UnityPerMaterial)
    float4 _DiffuseMap_ST;

    float _DayTime;
    float _RampV1;
    float _RampV2;
    float _RampV3;
    float _RampV4;
    float _RampV5;

    float _Diffuse_Intensity;
    float _Transition_Range;

    float _Specular_Intensity;
    float _Specular_Range;
    float _MetalSoftSpecToggle;

    float _Emission_Intensity;
    float3 _Emission_Color;
    float _Emission_Color_Only;

    float _GI_Intensity;

    float _Rim_Intensity;
    float2 _Space_1;
    float3 _Rim_Color;
    float _Rim_Scale;
    float _Rim_Clamp;

    float _Edge_Rim_Intensity;
    float _Edge_Rim_Threshold;
    float _Edge_Rim_Width;

    float4 _OutlineColor;
    float _OutlineWidth;

    float _ReceiveLightShadowsToggle;
    float _ReceiveDepthShadowsToggle;
    float _Cutoff;
    float _PreMulAlphaToggle;

    float3 _FrontDirection;
    float3 _RightDirection;
CBUFFER_END

half Alpha(half alpha, half cutoff, float2 uv)
{
#if defined(_SHADOWS_CLIP)
    clip(alpha - cutoff);
#elif defined(_SHADOWS_DITHER)
    clip(alpha - InterleavedGradientNoise(uv, 0));
#endif

    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
}

#endif

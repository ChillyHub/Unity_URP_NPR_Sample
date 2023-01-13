#ifndef SHADOW_CASTER_PASS_INCLUDED
#define SHADOW_CASTER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

#include "AvatarInput.hlsl"

float3 _LightDirection;
float3 _LightPosition;

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
};

float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

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

Varyings ShadowPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);

    output.uv = TRANSFORM_TEX(input.texcoord, _DiffuseMap);
    output.positionCS = GetShadowPositionHClip(input);
    return output;
}

void ShadowPassFragment(Varyings input)
{
    half alpha = 1.0;
#if defined(_IS_TRANSPARENT)
    alpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_DiffuseMap, sampler_DiffuseMap)).a;
#endif

    Alpha(alpha, _Cutoff, input.positionCS.xy);
}

#endif
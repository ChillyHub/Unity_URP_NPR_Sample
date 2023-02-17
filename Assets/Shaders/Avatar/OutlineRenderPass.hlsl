#ifndef OUTLINE_RENDER_PASS_INCLUDED
#define OUTLINE_RENDER_PASS_INCLUDED

#include "AvatarInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    float2 baseUV : TEXCOORD0;
    float2 smoothNormalOS : TEXCOORD1;
    float3 normalOS : NORMAL;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float4 color : VAR_COLOR;
    float2 baseUV : VAR_BASE_UV;
};

Varyings OutlineRenderPassVertex(Attributes input)
{
    Varyings output;

#if defined(_NORMAL_FIXED)
    half3 smoothNormalOS = UnpackNormalOctQuadEncode(input.smoothNormalOS);
    float3 normalWS = TransformObjectToWorldNormal(smoothNormalOS);
#else
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(_USE_VERTEX_ALPHA)
    float outlineWidth = _OutlineWidth * input.color.a;
#else
    float outlineWidth = _OutlineWidth;
#endif
    
    float3 normalCS = TransformWorldToHClipDir(normalWS, true);
    float4 outlineOffset = float4(normalCS.x, normalCS.y * (_ScreenParams.x / _ScreenParams.y), normalCS.z, 0.0);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionCS += outlineOffset * min(0.4, output.positionCS.w) * outlineWidth / 180.0;
    output.color = input.color;
    output.baseUV = input.baseUV;
    return output;
}

float4 OutlineRenderPassFragment(Varyings input) : SV_Target
{
#if !defined(_OUTLINE_ON)
    clip(-1.0);
#endif

#if defined(_USE_VERTEX_COLOR)
    half4 color = input.color;
    color.a = 1.0;
#else
    half4 color = SAMPLE_TEXTURE2D(_DiffuseMap, sampler_DiffuseMap, input.baseUV);
#endif
    
    return color * _OutlineColor;
}

#endif

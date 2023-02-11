#ifndef OUTLINE_RENDER_PASS_INCLUDED
#define OUTLINE_RENDER_PASS_INCLUDED

#include "AvatarInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 color : COLOR;
    float3 normalOS : NORMAL;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
};

Varyings OutlineRenderPassVertex(Attributes input)
{
    Varyings output;

#if defined(_NORMAL_FIXED)
    float3 normalWS = TransformObjectToWorldNormal(input.color);
#else
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif
    float3 normalCS = TransformWorldToHClipDir(normalWS, true);
    float4 outlineOffset = float4(normalCS.x, normalCS.y * (_ScreenParams.x / _ScreenParams.y), normalCS.z, 0.0);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionCS += outlineOffset * min(3.0, output.positionCS.w) * _OutlineWidth / 360.0;
    return output;
}

float4 OutlineRenderPassFragment(Varyings input) : SV_Target
{
#if !defined(_OUTLINE_ON)
    clip(-1.0);
#endif
    return _OutlineColor;
}

#endif

#ifndef GBUFFER_PASS_INCLUDED
#define GBUFFER_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityGBuffer.hlsl"

// keep this file in sync with LitForwardPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv                       : TEXCOORD0;
    half3 normalWS                  : TEXCOORD2;
};

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitGBufferPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = vertexInput.positionCS;
    output.uv = TRANSFORM_TEX(input.texcoord, _DiffuseMap);
    output.normalWS = normalInput.normalWS;

    return output;
}

// Used in Standard (Physically Based) shader
FragmentOutput LitGBufferPassFragment(Varyings input)
{
    half3 packedNormalWS = PackNormal(input.normalWS);

    FragmentOutput output = (FragmentOutput)0;
    output.GBuffer2 = half4(packedNormalWS, 0.0);

    return output;
}

#endif

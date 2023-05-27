#ifndef DEPTH_NORMALS_PASS_INCLUDED
#define DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float4 positionOS     : POSITION;
    float4 tangentOS      : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float3 normal       : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD1;
    half3 normalWS     : TEXCOORD2;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


Varyings DepthNormalsVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    output.uv         = TRANSFORM_TEX(input.texcoord, _DiffuseMap);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.normalWS = TransformObjectToWorldNormal(input.normal);

    return output;
}


half4 DepthNormalsFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    half alpha = 1.0;
    #if defined(_IS_TRANSPARENT)
        alpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_DiffuseMap, sampler_DiffuseMap)).a;
    #endif

    Alpha(alpha, _Cutoff, input.positionCS.xy);

    #if defined(_GBUFFER_NORMALS_OCT)
        float3 normalWS = normalize(input.normalWS);
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
        return half4(packedNormalWS, 0.0);
    #else

        return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
    #endif
}

#endif

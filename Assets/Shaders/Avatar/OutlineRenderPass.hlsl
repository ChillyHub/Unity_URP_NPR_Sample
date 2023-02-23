#ifndef OUTLINE_RENDER_PASS_INCLUDED
#define OUTLINE_RENDER_PASS_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
    float2 smoothNormalTS : TEXCOORD3;
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
    half3 normalOS = normalize(input.normalOS);
    half3 tangentOS = normalize(input.tangentOS.xyz);
    half3 bitangnetOS = normalize(cross(normalOS, tangentOS) * (input.tangentOS.w * GetOddNegativeScale()));
    half3 smoothNormalTS = UnpackNormalOctQuadEncode(input.smoothNormalTS);
    half3 smoothNormalOS = mul(smoothNormalTS, float3x3(tangentOS, bitangnetOS, normalOS));
    float3 normalWS = TransformObjectToWorldNormal(smoothNormalOS);
#else
    half3 smoothNormalOS = normalize(input.normalOS);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#endif

#if defined(_USE_VERTEX_ALPHA)
    float outlineWidth = _OutlineWidth * input.color.a;
#else
    float outlineWidth = _OutlineWidth;
#endif

    float3 offset = smoothNormalOS * outlineWidth * 0.001;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz + offset);
    output.color = input.color;
    output.baseUV = input.baseUV;
    return output;
    
    // float3 normalCS = TransformWorldToHClipDir(normalWS, true);
    // float4 outlineOffset = float4(normalCS.x, normalCS.y * (_ScreenParams.x / _ScreenParams.y), normalCS.z, 0.0);
    // outlineOffset = normalize(outlineOffset);
    // output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    // output.positionCS += outlineOffset * min(0.4, output.positionCS.w) * outlineWidth / 180.0;
    // output.color = input.color;
    // output.baseUV = input.baseUV;
    // return output;
}

float4 OutlineRenderPassFragment(Varyings input) : SV_Target
{
#if !defined(_OUTLINE_ON)
    clip(-1.0);
#endif

#if defined(_USE_VERTEX_COLOR)
    half4 color = input.color;
#else
    half4 color = SAMPLE_TEXTURE2D(_DiffuseMap, sampler_DiffuseMap, input.baseUV);
#endif
    color.a = 1.0;
    
    return color * _OutlineColor;
}

#endif

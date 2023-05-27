#ifndef GRAN_TURISMO_TONE_MAPPING_PASS_INCLUDED
#define GRAN_TURISMO_TONE_MAPPING_PASS_INCLUDED

TEXTURE2D(_SourceTex);
SAMPLER(sampler_SourceTex);

CBUFFER_START(UnityPerMaterial)
    float _P;                    // Maximum brightness
    float _a;                    // Slope
    float _m;                    // Linear section start
    float _l;                    // Linear section length
    float _c;                    // Black tightness
    float _b;                    // Darkness value
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

// Copyright(c) 2017 by Hajime UCHIMURA @ Polyphony Digital Inc.
void GranTurismoMapper(inout float3 color)
{
    // Toe
    float3 T = _m * pow(color * rcp(_m), _c) + _b;
    // Linear
    float l0 = (_P - _m) * _l * rcp(_a);
    float3 L = _m + _a * (color - _m);
    // Shoulder
    float S0 = _m + l0;
    float S1 = _m + l0 * _a;
    float C2 = _a * _P * rcp(_P - S1);
    float3 S = _P - (_P - S1) * exp(-C2 * (color - S0) * rcp(_P));

    // Weight
    float3 w0 = 1.0 - smoothstep(0.0, _m, color);
    float3 w2 = step(_m + l0, color);
    float3 w1 = 1.0 - w0 - w2;

    color = T * w0 + L * w1 + S * w2;
}

Varyings GranTurismoToneMappingPassVertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.screenUV = input.baseUV;

    return output;
}

float4 GranTurismoToneMappingPassFragment(Varyings input) : SV_Target
{
    float4 rgba = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, input.screenUV);
    float3 color = rgba.rgb;

    GranTurismoMapper(color);

    return float4(color, rgba.a);
}

#endif
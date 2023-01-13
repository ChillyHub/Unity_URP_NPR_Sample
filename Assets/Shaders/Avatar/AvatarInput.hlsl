#ifndef AVATAR_INPUT_INCLUDED
#define AVATAR_INPUT_INCLUDED

#include "../Utility.hlsl"

//struct Attributes
//{
//    float4 positionOS : POSITION;
//    float3 color : COLOR;
//    float2 baseUV : TEXCOORD0;
//    float3 normalOS : NORMAL;
//    float4 tangentOS : TANGENT;
//};

TEXTURE2D(_DiffuseMap);
SAMPLER(sampler_DiffuseMap);
TEXTURE2D(_LightMap);
SAMPLER(sampler_LightMap);
TEXTURE2D(_RampMap);
SAMPLER(sampler_RampMap);
TEXTURE2D(_MetalMap);
SAMPLER(sampler_MetalMap);
TEXTURE2D(_FaceLightMap);
SAMPLER(sampler_FaceLightMap);

CBUFFER_START(UnityPerMaterial)
    float4 _DiffuseMap_ST;
    float4 _RampMap_TexelSize;
    float _AO_Strength;
    float _Transition_Range;
    float _Specular_Range;
    float _Specular_Strength;
    float _Emission_Strength;
    float _GI_Strength;

    float _NightToggle;

    float _Space_1;

    float3 _Rim_Color;
    float _Rim_Strength;
    float _Rim_Scale;
    float _Rim_Clamp;
    
    float2 _Space_2;

    float4 _OutlineColor;
    float _OutlineWidth;

    float _ReceiveShadowsToggle;
    float _Cutoff;
    float _PreMulAlphaToggle;
CBUFFER_END

#endif

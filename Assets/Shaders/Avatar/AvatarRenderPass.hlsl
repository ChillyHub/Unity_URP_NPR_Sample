#ifndef AVATAR_RENDER_PASS_INCLUDED
#define AVATAR_RENDER_PASS_INCLUDED

#include "AvatarInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    half3 normalWS : VAR_NORMAL;
    half4 tangentWS : VAR_TANGENT;

#if defined(_IS_FACE)
    float3 frontWS : VAR_FRONT;
    float3 rightWS : VAR_RIGHT;
#endif
};

Varyings HairRenderPassVertex(Attributes input)
{
    Varyings output;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.baseUV = TRANSFORM_TEX(input.baseUV, _DiffuseMap);
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

#if defined(_IS_FACE)
    output.frontWS = TransformObjectToWorld(float3(0.0, 0.0, 1.0));
    output.rightWS = TransformObjectToWorld(float3(1.0, 0.0, 0.0));
#endif

    return output;
}

float4 HairRenderPassFragment(Varyings input) : SV_Target
{
    half4 diffuseMap = SAMPLE_TEXTURE2D(_DiffuseMap, sampler_DiffuseMap, input.baseUV);
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.baseUV);

    half3 diffuseColor = diffuseMap.rgb;

#if defined(_IS_OPAQUE)
    half emissionFac = diffuseMap.a;
    half alpha = 1.0;
#else
    half emissionFac = 0.0;
    half alpha = diffuseMap.a;
#endif

    diffuseColor = lerp(diffuseColor, diffuseColor * alpha, _PreMulAlphaToggle);

    Light light = GetMainLight();
    float3 lightDir = normalize(light.direction);
    float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
    float3 halfDir = normalize(lightDir + viewDir);
    float NdotL = dot(input.normalWS, lightDir);
    float NdotV = dot(input.normalWS, viewDir);
    float NdotH = dot(input.normalWS, halfDir);

#if defined(_IS_FACE)
    half material = lightMap.r;
    half ambientOcclusion = lightMap.b;
    
    float3 lightDirOSXZ = normalize(float3(dot(input.rightWS, lightDir), 0.0, dot(input.frontWS, lightDir)));
    float lightFac = dot(lightDirOSXZ, float3(0.0, 0.0, 1.0)) * 0.5 + 0.5;
    half4 faceLightMapR = SAMPLE_TEXTURE2D(_FaceLightMap, sampler_FaceLightMap, input.baseUV);
    half4 faceLightMapL = SAMPLE_TEXTURE2D(_FaceLightMap, sampler_FaceLightMap,
        float2(1.0 - input.baseUV.x, input.baseUV.y));
    half faceShadowFac = lerp(faceLightMapL.r, faceLightMapR.r, step(0, dot(lightDirOSXZ, input.rightWS)));
    
    float halfLambert = smoothstep(faceShadowFac - 0.001, faceShadowFac, lightFac);
#else
    half metalic = lightMap.r;
    half ambientOcclusion = lightMap.g;
    half specularFac = lightMap.b;
    half material = lightMap.a;

    float halfLambert = (NdotL * _Transition_Range) * 0.5 + 0.5;
#endif
    
    float offsetX = halfLambert * 2;// smoothstep(_Transition_Range, 1.0, halfLambert * 2.0);
    float offsetY = material * 0.5 + lerp(0.5, 0.0, _NightToggle);

#if defined(_DIFFUSE_ON)

#if defined(_TRANSITION_BLUR)
    float offsetFrac = frac(offsetX / _RampMap_TexelSize.x);
    float leftOrRight = step(0.5, offsetFrac);
    float offsetXL = lerp(offsetX - _RampMap_TexelSize.x, offsetX, leftOrRight);
    float offsetXR = lerp(offsetX, offsetX + _RampMap_TexelSize.x, leftOrRight);

    half3 rampColorL = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(min(offsetXL, 1.0), offsetY)).xyz;
    half3 rampColorR = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(min(offsetXR, 1.0), offsetY)).xyz;
    half3 rampColor = lerp(rampColorL, rampColorR, offsetFrac + lerp(0.5, -0.5, leftOrRight));
#else
    half3 rampColor = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(min(offsetX, 1.0), offsetY)).xyz;
#endif
    
    rampColor = lerp(rampColor, float3(1.0, 1.0, 1.0), smoothstep(0.8, 1.2, offsetX));
    rampColor = rampColor * lerp(1.0, ambientOcclusion, _AO_Strength);

    float3 diffuse = diffuseColor * rampColor * light.color * light.distanceAttenuation;
#else
    float3 diffuse = float3(0.0, 0.0, 0.0);
#endif

#if defined(_SPECULAR_ON) && !defined(_IS_FACE)
    float3 normalCS = TransformWorldToViewDir(input.normalWS, true);
    float2 metalUV = normalCS.xy * 0.5 + 0.5;
    float metalFac = SAMPLE_TEXTURE2D(_MetalMap, sampler_MetalMap, metalUV).r;

    float3 viewSpec = lerp(0.0, smoothstep(1.0 - _Specular_Range / 16.0, 1.0, NdotV), smoothstep(0.2, 0.8, metalic));
    float3 blinSpec = lerp(0.0, pow(max(NdotH, 0.0), 17.0 - _Specular_Range), smoothstep(0.7, 1.0, metalic)) * metalFac;

    float3 specular = diffuseColor * light.color * light.distanceAttenuation *
        (viewSpec + blinSpec) * specularFac * smoothstep(0.5, 0.55, halfLambert);
#else
    float3 specular = 0.0;
#endif

    
#if defined(_GI_ON)
    float3 ambient = diffuseColor * SampleLightProbe(input.positionWS, input.normalWS) * _GI_Strength;
#else
    float3 ambient = float3(0.0, 0.0, 0.0);
#endif

#if defined(_EMISSION_ON)
    float3 emission = diffuseColor * emissionFac * _Emission_Strength;
#else
    float3 emission = float3(0.0, 0.0, 0.0);
#endif

#if defined(_RIM_ON)
    float3 rim = _Rim_Strength * _Rim_Color * max(_Rim_Clamp, pow((1.0 - max(NdotV, 0.0)), 0.5 / _Rim_Scale));
#else
    float3 rim = float3(0.0, 0.0, 0.0);
#endif
    
    return float4(diffuse + specular + ambient + emission + rim, alpha);
}

#endif

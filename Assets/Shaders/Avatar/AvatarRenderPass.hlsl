#ifndef AVATAR_RENDER_PASS_INCLUDED
#define AVATAR_RENDER_PASS_INCLUDED

#include "AvatarInput.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float3 color : COLOR;
    float2 baseUV : TEXCOORD0;
    float2 smoothNormalOS : TEXCOORD1;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    half3 normalWS : VAR_NORMAL;
    half3 tangentWS : VAR_TANGENT;
    half3 bitangentWS : VAR_BITANGENT;
    half3 smoothNormalWS : VAR_SMOOTHNORMAL;

    half3 color : VAR_COLOR;

#if defined(_IS_FACE)
    half3 frontWS : VAR_FRONT;
    half3 rightWS : VAR_RIGHT;
#endif
};

struct Surface
{
    // Geometry
    float4 positionCS;
    float3 positionWS;
    float2 baseUV;
    half3 normalWS;
    half3 smoothNormalWS;
    half3 lightDirWS;
    half3 viewDirWS;
    half3 halfDirWS;
    half NdotL;
    half NdotV;
    half NdotH;
    half LdotV;

    // Material
    float3 diffuseColor;
    half alpha;
    half material;
    half ambientOcclusion;
    half diffuseFac;
    half specularFac;
    half emissionFac;
    half specularThreshold;
    half isMetal;
};

half GetRampV(half mat)
{
    return lerp(lerp(lerp(lerp(
        _RampV1,
        _RampV2, step(0.2, mat)),
        _RampV3, step(0.4, mat)),
        _RampV4, step(0.6, mat)),
        _RampV5, step(0.8, mat));
}

Surface GetBodySurface(float4 diffuseMap, float4 normalMap, float4 lightMap, Light light, Varyings input,
    float isPreMulAlpha = false)
{
    Surface o;
    half3 normalTS = UnpackNormal(normalMap);
    o.positionCS = input.positionCS;
    o.positionWS = input.positionWS;
    o.baseUV = input.baseUV;
    o.normalWS = mul(normalTS, float3x3(input.tangentWS, input.bitangentWS, input.normalWS));
    o.smoothNormalWS = mul(normalTS, float3x3(input.tangentWS, input.bitangentWS, input.smoothNormalWS));
    o.lightDirWS = normalize(light.direction);
    o.viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
    o.halfDirWS = normalize(o.lightDirWS + o.viewDirWS);
    o.NdotL = dot(o.normalWS, o.lightDirWS);
    o.NdotV = dot(o.normalWS, o.viewDirWS);
    o.NdotH = dot(o.normalWS, o.halfDirWS);
    o.LdotV = dot(o.lightDirWS, o.viewDirWS);
    
    o.diffuseColor = diffuseMap.rgb;
    o.material = lightMap.a;
    o.ambientOcclusion = lightMap.g;
    o.diffuseFac = o.NdotL * 0.5 + 0.5;
    o.specularFac = lightMap.r;
    o.specularThreshold = 1.0 - lightMap.b;
    o.isMetal = step(0.95, o.specularFac);

#if defined(_IS_OPAQUE)
    o.emissionFac = diffuseMap.a;
    o.alpha = 1.0;
#else
    o.emissionFac = 0.0;
    o.alpha = diffuseMap.a;
#endif

    o.diffuseColor = lerp(o.diffuseColor, o.diffuseColor * o.alpha, isPreMulAlpha);

    return o;
}

Surface GetFaceSurface(float4 diffuseMap, float4 normalMap, float4 lightMap,
    TEXTURE2D(faceLightmap), SAMPLER(samplerFaceLightmpa), Light light, Varyings input, half3 rightWS, half3 frontWS,
    float isPreMulAlpha = false)
{
    Surface o = GetBodySurface(diffuseMap, normalMap, lightMap, light, input, isPreMulAlpha);
    o.material = lightMap.r;
    o.ambientOcclusion = lightMap.b;
    o.specularFac = 0.0;
    o.specularThreshold = 1.0;
    o.isMetal = 0.0;

    half3 lightDirOSXZ = normalize(-half3(dot(rightWS, o.lightDirWS), 0.0, dot(frontWS, o.lightDirWS)));
    half2 lightMapUV = lerp(half2(1.0 - input.baseUV.x, input.baseUV.y), input.baseUV,
        step(0, dot(lightDirOSXZ, rightWS)));

    half lightFac = dot(lightDirOSXZ, float3(0.0, 0.0, 1.0)) * 0.5 + 0.5;
    half faceShadowFac = SAMPLE_TEXTURE2D(faceLightmap, samplerFaceLightmpa, lightMapUV).r;

    o.diffuseFac = smoothstep(lightFac, lightFac + 0.01, faceShadowFac);

    return o;
}

half GetRampU(Surface surface, half shadow, half range)
{
#if defined(_IS_FACE)
    half rampU = 1.0;
#else
    half rampU = saturate(min(min(surface.ambientOcclusion * 1.5, surface.diffuseFac), shadow) * 2.0);
    rampU = range + (1.0 - range) * rampU;
#endif

    return rampU;
}

half3 MixRampColor(Surface surface, Light light, half3 rampColorDay, half3 rampColorNight,
    half3 rampColorDarkDay, half3 rampColorDarkNight, half dayOrNight, half offsetU)
{
    half3 rampColorDark = lerp(rampColorDarkDay, rampColorDarkNight, dayOrNight);

#if defined(_IS_FACE)    
    half3 rampColor = lerp(rampColorDark, float3(1.0, 1.0, 1.0), surface.diffuseFac);
#else
    half3 rampColor = lerp(rampColorDay, rampColorNight, dayOrNight);
    rampColor = lerp(rampColor, float3(1.0, 1.0, 1.0), step(0.99, offsetU));
#endif
    
    rampColor = lerp(rampColorDark, rampColor, saturate(light.color * light.distanceAttenuation * 2.0 - 1.0));
    rampColor = lerp(rampColor, half3(0.2, 0.2, 0.2), surface.isMetal);

    return rampColor;
}

float3 GetDiffuse(TEXTURE2D(rampMap), SAMPLER(samplerRampMap), Surface surface, Light light, float dayTime,
    float transitionRange, float isReceiveLightShadows = false, float isReceiveDepthShadows = false)
{
#if defined(_DIFFUSE_ON)
    half shadow = lerp(1.0, light.shadowAttenuation, isReceiveLightShadows);
    half offsetU = GetRampU(surface, shadow, transitionRange);
    half offsetV = GetRampV(surface.material);
    half offsetVDay = offsetV * 0.1 + 0.45;
    half offsetVNight = offsetV * 0.1 - 0.05;
    half dayOrNight = smoothstep(4.0, 8.0, abs(dayTime - 12.0));
    half3 rampColorDarkDay = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(0.0, offsetVDay)).rgb;
    half3 rampColorDarkNight = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(0.0, offsetVNight)).rgb;
    half3 rampColorDay = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(offsetU, offsetVDay)).rgb;
    half3 rampColorNight = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(offsetU, offsetVNight)).rgb;
    half3 rampColor = MixRampColor(surface, light,
        rampColorDay, rampColorNight, rampColorDarkDay, rampColorDarkNight, dayOrNight, offsetU);
    
    float3 diffuse = surface.diffuseColor * rampColor;
#else
    float3 diffuse = float3(0.0, 0.0, 0.0);
#endif

    return diffuse;
}

float3 GetSpecular(TEXTURE2D(metalMap), SAMPLER(samplerMetalMap), Surface surface, Light light, float specularRange,
    float isMetalSoftSpec)
{
#if defined(_SPECULAR_ON) && !defined(_IS_FACE)
    float3 normalVS = TransformWorldToViewDir(surface.normalWS, true);
    float2 metalUV = float2(normalVS.x * 0.6 + 0.5, normalVS.y * 0.5 + 0.5);
    half metalFac = SAMPLE_TEXTURE2D(metalMap, samplerMetalMap, metalUV).r;
    half blinFac = pow(saturate(surface.NdotH), specularRange);
    
    half isSoftSpec = isMetalSoftSpec * surface.isMetal;
    half3 blinSpec = smoothstep(surface.specularThreshold,
        lerp(surface.specularThreshold + 0.01, surface.specularThreshold + 0.4, isSoftSpec), blinFac);
    half3 metalSpec = surface.diffuseColor * lerp(0.2, 1.0, step(0.5, surface.diffuseFac)) * metalFac * surface.isMetal;

    half3 blinSpecColor = lerp(surface.diffuseColor, half3(1.0, 1.0, 1.0), surface.isMetal);
    float3 specular = light.color * light.distanceAttenuation *
        (blinSpec * blinSpecColor + metalSpec) * surface.specularFac;
#else
    float3 specular = float3(0.0, 0.0, 0.0);
#endif

    return specular;
}

float3 GetGI(Surface surface)
{
#if defined(_GI_ON)
    float3 ambient = surface.diffuseColor * SampleLightProbe(surface.positionWS, surface.normalWS);
#else
    float3 ambient = float3(0.0, 0.0, 0.0);
#endif

    return ambient;
}

float3 GetEmission(Surface surface, float3 emissionColor, float colorOnly)
{
#if defined(_EMISSION_ON)
    float3 emission = lerp(surface.diffuseColor * emissionColor, emissionColor, colorOnly) * surface.emissionFac;
#else
    float3 emission = float3(0.0, 0.0, 0.0);
#endif

    return emission;
}

float3 GetFresnelRim(Surface surface, float3 rimColor, half rimScale, half rimClamp)
{
#if defined(_RIM_ON)
    float3 fresnelRim = rimColor * max(rimClamp, pow(1.0 - max(min(surface.NdotV, 1.0), 0.0), 0.5 / rimScale));
#else
    float3 fresnelRim = float3(0.0, 0.0, 0.0);
#endif

    return fresnelRim;
}

float3 GetEdgeRim(Surface surface, float3 diffuse, float threshold, float width)
{
#if !defined(_DIFFUSE_ON)
    diffuse = float3(1.0, 1.0, 1.0);
#endif
    
#if defined(_EDGE_RIM_ON)
    float2 bias = TransformWorldToViewDir(surface.normalWS).xy * width / 360.0;
    float2 trueUV = GetNormalizedScreenSpaceUV(surface.positionCS);
    float2 biasUV = trueUV + bias;

    float depthTrue = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, trueUV);
    float depthBias = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, biasUV);
    float linearDepthTrue = LinearEyeDepth(depthTrue, _ZBufferParams);
    float linearDepthBias = LinearEyeDepth(depthBias, _ZBufferParams);

    float isEdge = step(threshold, linearDepthBias - linearDepthTrue);
    float strength = min(linearDepthBias - linearDepthTrue, 1.0) * (surface.LdotV * -0.5 + 0.5);
    float3 edgeRim = strength * diffuse * isEdge;
#else
    float3 edgeRim = float3(0.0, 0.0, 0.0);
#endif

    return edgeRim;
}

Varyings HairRenderPassVertex(Attributes input)
{
    Varyings output;
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.baseUV = TRANSFORM_TEX(input.baseUV, _DiffuseMap);

    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = normalInputs.tangentWS;
    output.bitangentWS = normalInputs.bitangentWS;
    
#if defined(_NORMAL_FIXED)
    half3 smoothNormalOS = UnpackNormalOctQuadEncode(input.smoothNormalOS);
    output.smoothNormalWS = TransformObjectToWorldNormal(smoothNormalOS);
#else
    output.smoothNormalWS = output.normalWS;
#endif

#if defined(_IS_FACE)
    output.frontWS = TransformObjectToWorldDir(float3(0.0, 0.0, 1.0));
    output.rightWS = TransformObjectToWorldDir(float3(-1.0, 0.0, 0.0));
#endif

    output.color = input.color;

    return output;
}

float4 HairRenderPassFragment(Varyings input) : SV_Target
{
    half4 diffuseMap = SAMPLE_TEXTURE2D(_DiffuseMap, sampler_DiffuseMap, input.baseUV);
    half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.baseUV);
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.baseUV);

    Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));

#if defined(_IS_FACE)
    Surface surface = GetFaceSurface(diffuseMap, normalMap, lightMap, _FaceLightMap, sampler_FaceLightMap,
        light, input, input.rightWS, input.frontWS);
#else
    Surface surface = GetBodySurface(diffuseMap, normalMap, lightMap, light, input, _PreMulAlphaToggle);
#endif

    float3 diffuse = _Diffuse_Intensity * GetDiffuse(_RampMap, sampler_RampMap, surface, light, _DayTime,
        _Transition_Range, _ReceiveLightShadowsToggle, _ReceiveDepthShadowsToggle);
    float3 specular = _Specular_Intensity * GetSpecular(_MetalMap, sampler_MetalMap, surface, light,
        _Specular_Range, _MetalSoftSpecToggle);
    float3 GI = _GI_Intensity * GetGI(surface);
    float3 emission = _Emission_Intensity * GetEmission(surface, _Emission_Color, _Emission_Color_Only);
    float3 fresnelRim = _Rim_Intensity * GetFresnelRim(surface, _Rim_Color, _Rim_Scale, _Rim_Clamp);
    float3 edgeRim = _Edge_Rim_Intensity * GetEdgeRim(surface, diffuse, _Edge_Rim_Threshold, _Edge_Rim_Width);
    
    return float4(diffuse + specular + GI + emission + fresnelRim + edgeRim, surface.alpha);
}

#endif

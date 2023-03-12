#ifndef AVATAR_FUNCTION_INCLUDED
#define AVATAR_FUNCTION_INCLUDED

half2 GetRampVDayNight(half mat)
{
    half v = 0.1 * lerp(lerp(lerp(lerp(
        _RampV1,
        _RampV2, step(0.2, mat)),
        _RampV3, step(0.4, mat)),
        _RampV4, step(0.6, mat)),
        _RampV5, step(0.8, mat));

    return half2(v + 0.45, v - 0.05);
}

float3 GetDiffuse(TEXTURE2D(rampMap), SAMPLER(samplerRampMap), Surface surface, Light light, float dayTime,
    float transitionRange, float isReceiveLightShadows = 0.0, float isReceiveDepthShadows = 0.0)
{
#if defined(_DIFFUSE_ON)
    half shadow = lerp(1.0, light.shadowAttenuation, isReceiveLightShadows);
    half offsetU = GetRampU(surface, shadow, transitionRange);
    half2 offsetV = GetRampVDayNight(surface.material);
    half offsetVDay = offsetV.x;
    half offsetVNight = offsetV.y;
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
#if defined(_SPECULAR_ON)
    float3 normalVS = TransformWorldToViewDir(surface.normalWS, true);
    float2 metalUV = float2(normalVS.x * 0.6 + 0.5, normalVS.y * 0.5 + 0.5);
    half metalFac = SAMPLE_TEXTURE2D(metalMap, samplerMetalMap, metalUV).r;
    half blinFac = pow(saturate(surface.NdotH), specularRange);
    
    half isSoftSpec = isMetalSoftSpec * surface.isMetal;
    half3 blinSpec = smoothstep(surface.specularThreshold,
        lerp(surface.specularThreshold + 0.001, surface.specularThreshold + 0.4, isSoftSpec), blinFac);
    half3 metalSpec = surface.diffuseColor * lerp(0.2, 1.0, step(0.5, surface.diffuseFac)) * metalFac * surface.isMetal;

    half3 blinSpecColor = lerp(surface.diffuseColor, half3(0.7, 0.7, 0.7), surface.isMetal);
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

#endif

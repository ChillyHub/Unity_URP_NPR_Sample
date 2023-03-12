#ifndef AVATAR_FACE_RENDER_PASS_INCLUDED
#define AVATAR_FACE_RENDER_PASS_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color : COLOR;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    float2 baseUV : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionVS : VAR_POSITION_VS;
    float3 positionWS : VAR_POSITION_WS;
    float4 positionNDC : VAR_POSITION_NDC;
    float2 baseUV : VAR_BASE_UV;
    half3 color : VAR_COLOR;
    half3 normalWS : VAR_NORMAL;
    half3 tangentWS : VAR_TANGENT;
    half3 bitangentWS : VAR_BITANGENT;
    
    half3 frontWS : VAR_FRONT;
    half3 rightWS : VAR_RIGHT;
};

struct Surface
{
    // Geometry
    float4 positionCS;
    float3 positionWS;
    float3 positionVS;
    float4 positionNDC;
    float2 baseUV;
    half3 normalWS;
    half3 lightDirWS;
    half3 viewDirWS;
    half3 halfDirWS;
    half NdotL;
    half NdotV;
    half NdotH;
    half LdotV;
    
    half FdotV;

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

float GetDepthShadow(float3 positionWS, float3 normalWS, float3 lightDir, float3 frontDir)
{
#if defined(_RECEIVE_DEPTH_SHADOWS)
    if (dot(frontDir, lightDir) < 0.86)
    {
        float3 rightDir = cross(frontDir, lightDir);
        float3 upDir = cross(rightDir, frontDir);
        lightDir = normalize(frontDir + 0.57 * upDir);
    }

    float stepLength = 0.001;
    float3 currPos = positionWS + stepLength * lightDir;

    UNITY_LOOP
    for (int i = 0; i < 30; i++)
    {
        float3 currPosVS = TransformWorldToView(currPos);
        float4 currPosCS = TransformWViewToHClip(currPosVS);
        float2 currUV = ComputeNormalizedDeviceCoordinates(currPosCS.xyz / currPosCS.w);
        
        float currDepthMap = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, currUV);
        float currObjDepth = LinearEyeDepth(currDepthMap, _ZBufferParams);
        float currEyeDepth = abs(currPosVS.z);

        UNITY_BRANCH
        if (step(0.0, currEyeDepth - currObjDepth))
        {
            return 0.0;
        }

        currPos += stepLength * lightDir;
    }
#endif

    return 1.0;
}

Surface GetSurface(float4 diffuseMap, float4 normalMap, float4 lightMap, float3 blushColor, float blushIntensity, 
    TEXTURE2D(faceLightmap), SAMPLER(samplerFaceLightmpa), Light light, Varyings input, half3 rightWS, half3 frontWS,
    float isPreMulAlpha = false)
{
    Surface o;
    half3 normalTS = UnpackNormal(normalMap);
    o.positionCS = input.positionCS;
    o.positionWS = input.positionWS;
    o.positionVS = input.positionVS;
    o.positionNDC = input.positionNDC;
    o.baseUV = input.baseUV;
    o.normalWS = mul(normalTS, float3x3(input.tangentWS, input.bitangentWS, input.normalWS));
    o.lightDirWS = normalize(light.direction);
    o.viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
    o.halfDirWS = normalize(o.lightDirWS + o.viewDirWS);
    o.NdotL = dot(o.normalWS, o.lightDirWS);
    o.NdotV = dot(o.normalWS, o.viewDirWS);
    o.NdotH = dot(o.normalWS, o.halfDirWS);
    o.LdotV = dot(o.lightDirWS, o.viewDirWS);

#if defined(_IS_OPAQUE)
    o.emissionFac = diffuseMap.a;
    o.alpha = 1.0;
#else
    o.emissionFac = 0.0;
    o.alpha = diffuseMap.a;
#endif

#if defined(_BLUSH_ON)
    o.diffuseColor = lerp(diffuseMap.rgb, blushColor, blushIntensity * o.emissionFac);
#else
    o.diffuseColor = diffuseMap.rgb;
#endif
    
    o.diffuseColor = lerp(o.diffuseColor, o.diffuseColor * o.alpha, isPreMulAlpha);
    
    o.material = lightMap.r;
    o.ambientOcclusion = lightMap.b;
    o.specularFac = 0.0;
    o.specularThreshold = 1.0;
    o.isMetal = 0.0;

    half3 lightDirOSXZ = normalize(-half3(dot(rightWS, o.lightDirWS), 0.0, dot(frontWS, o.lightDirWS)));
    half2 lightMapUV = lerp(half2(1.0 - input.baseUV.x, input.baseUV.y), input.baseUV, step(0, lightDirOSXZ.x));

    half lightFac = dot(lightDirOSXZ, float3(0.0, 0.0, 1.0)) * 0.5 + 0.5;
    half faceShadowFac = SAMPLE_TEXTURE2D(faceLightmap, samplerFaceLightmpa, lightMapUV).r;

    o.diffuseFac = smoothstep(lightFac, lightFac + 0.001, faceShadowFac) *
        GetDepthShadow(o.positionWS, o.normalWS, light.direction, frontWS);
    
    o.FdotV = dot(frontWS, o.viewDirWS);

    return o;
}

half GetRampU(Surface surface, half shadow, half range)
{
    return 1.0;
}

half3 MixRampColor(Surface surface, Light light, half3 rampColorDay, half3 rampColorNight,
    half3 rampColorDarkDay, half3 rampColorDarkNight, half dayOrNight, half offsetU)
{
    half3 rampColorDark = lerp(rampColorDarkDay, rampColorDarkNight, dayOrNight);
    
    half3 rampColor = lerp(rampColorDark, float3(1.0, 1.0, 1.0), surface.diffuseFac);

    half3 fac = lerp(saturate(light.color * light.distanceAttenuation * 2.0 - 1.0),
        light.shadowAttenuation, _ReceiveLightShadowsToggle);
    rampColor = lerp(rampColorDark, rampColor, fac);

    return rampColor;
}

float3 GetEdgeRim(Surface surface, float3 diffuse, float threshold, float width)
{
#if !defined(_DIFFUSE_ON)
    diffuse = float3(1.0, 1.0, 1.0);
#endif
    
    half faceIntensity = saturate(1.0 - surface.FdotV * 1.5);
    
#if defined(_EDGE_RIM_ON)
    float3 biasVS = TransformWorldToViewDir(surface.normalWS) * width * 0.003;
    float3 biasPosVS = surface.positionVS + biasVS;
    float4 biasPosCS = TransformWViewToHClip(biasPosVS);
    half2 trueUV = GetNormalizedScreenSpaceUV(surface.positionCS);
    half2 biasUV = ComputeNormalizedDeviceCoordinates(biasPosCS.xyz / biasPosCS.w);

    float depthTrue = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, trueUV);
    float depthBias = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, biasUV);
    float linearDepthTrue = LinearEyeDepth(depthTrue, _ZBufferParams);
    float linearDepthBias = LinearEyeDepth(depthBias, _ZBufferParams);

    float isEdge = step(threshold, linearDepthBias - linearDepthTrue);
    float strength = min(linearDepthBias - linearDepthTrue, 1.0) * (surface.LdotV * -0.5 + 0.5) * faceIntensity;
    float3 edgeRim = strength * diffuse * isEdge;
#else
    float3 edgeRim = float3(0.0, 0.0, 0.0);
#endif

    return edgeRim;
}

#include "Assets/Shaders/Avatar/AvatarFunction.hlsl"

Varyings HairRenderPassVertex(Attributes input)
{
    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    Varyings output;
    output.positionWS = positionInputs.positionWS;
    output.positionVS = positionInputs.positionVS;
    output.positionCS = positionInputs.positionCS;
    output.positionNDC = positionInputs.positionNDC;
    output.baseUV = TRANSFORM_TEX(input.baseUV, _DiffuseMap);
    output.color = input.color.rgb;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = normalInputs.tangentWS;
    output.bitangentWS = normalInputs.bitangentWS;
    
    output.frontWS = TransformObjectToWorldDir(_FrontDirection);
    output.rightWS = TransformObjectToWorldDir(_RightDirection);

    return output;
}

float4 HairRenderPassFragment(Varyings input) : SV_Target
{
    half4 diffuseMap = SAMPLE_TEXTURE2D(_DiffuseMap, sampler_DiffuseMap, input.baseUV);
    half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, input.baseUV);
    half4 lightMap = SAMPLE_TEXTURE2D(_LightMap, sampler_LightMap, input.baseUV);

    Light light = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
    
    Surface surface = GetSurface(diffuseMap, normalMap, lightMap, _Emission_Color, _Emission_Intensity, 
        _FaceLightMap, sampler_FaceLightMap, light, input, input.rightWS, input.frontWS);

    float3 diffuse = _Diffuse_Intensity * GetDiffuse(_RampMap, sampler_RampMap, surface, light, _DayTime,
        _Transition_Range, _ReceiveLightShadowsToggle, _ReceiveDepthShadowsToggle);
    float3 GI = _GI_Intensity * GetGI(surface);
    float3 fresnelRim = _Rim_Intensity * GetFresnelRim(surface, _Rim_Color, _Rim_Scale, _Rim_Clamp);
    float3 edgeRim = _Edge_Rim_Intensity *
        GetEdgeRim(surface, diffuse, _Edge_Rim_Threshold, _Edge_Rim_Width) * light.color;
    
    return float4(diffuse + GI + fresnelRim + edgeRim, surface.alpha);
}

#endif

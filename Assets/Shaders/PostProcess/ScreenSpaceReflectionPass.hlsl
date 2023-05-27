#ifndef SCREEN_SPACE_REFLECTION_PASS_INCLUDED
#define SCREEN_SPACE_REFLECTION_PASS_INCLUDED

#include "Assets/Shaders/ShaderLibrary/Utility.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 baseUV : TEXCOORD0;
    float3 viewRay : TEXCOORD1;
};

TEXTURE2D(_SourceTex);
SAMPLER(sampler_SourceTex);
TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);
TEXTURE2D(_CameraNormalsTexture); 
SAMPLER(sampler_CameraNormalsTexture);

CBUFFER_START(UnityPerMaterial)
    float4x4 _WorldToViewMatrix;
    float4x4 _ViewToHClipMatrix;
    float4 _ProjectBufferParams;
    float _StopIndex;
    float _MaxLoop;
    float _Thickness;
    float _MaxStepSize;
CBUFFER_END

#ifdef _GBUFFER_NORMALS_OCT
half3 PackNormal(half3 n)
{
    float2 octNormalWS = PackNormalOctQuadEncode(n);                  // values between [-1, +1], must use fp32 on some platforms.
    float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0, +1]
    return half3(PackFloat2To888(remappedOctNormalWS));               // values between [ 0, +1]
}

half3 UnpackNormal(half3 pn)
{
    half2 remappedOctNormalWS = half2(Unpack888ToFloat2(pn));          // values between [ 0, +1]
    half2 octNormalWS = remappedOctNormalWS.xy * half(2.0) - half(1.0);// values between [-1, +1]
    return half3(UnpackNormalOctQuadEncode(octNormalWS));              // values between [-1, +1]
}

#else
half3 PackNormal(half3 n)
{ return n; }                                                         // values between [-1, +1]

half3 UnpackNormal(half3 pn)
{ return pn; }                                                        // values between [-1, +1]
#endif

float3 GetViewRay(float2 uv)
{
    float depth = 1.0;
#if defined(UNITY_REVERSED_Z)
    depth = 0.0;
#endif

    // In order to recreate world position from depth map
    float3 positionWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
    return positionWS - _WorldSpaceCameraPos.xyz;
}

bool IntersectPoint(out float3 intersect, float3 p1, float3 v1, float3 p2, float3 v2)
{
    intersect = 0.0;

    float3 v3 = p2 - p1;
    float3 s2 = cross(v1, v2);
    float3 s3 = cross(v3, v1);

    // Check whether v1 will intersect v2 
    UNITY_BRANCH
    if (dot(s2, s3) < 0.0)
    {
        return false;
    }

    float t = dot(s2, s3) / dot(s2, s2);

    intersect = p2 + v2 * t;
    return true;
}

bool SampleReflectionColorSlow(out half3 color, float2 uv, float3 normalWS, float3 rayDirWS)
{
    UNITY_BRANCH
    if (dot(normalWS, normalWS) < FLT_EPS)
    {
        return false;
    }

    float3 viewDirWS = normalize(rayDirWS);
    float3 reflectWS = reflect(viewDirWS, normalWS);
    
    float posZ = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    float depth = Linear01Depth(posZ, _ZBufferParams);
    float3 positionWS = GetCameraPositionWS() + depth * rayDirWS;
    float3 deltaPosWS = positionWS + reflectWS;
    float3 positionVS = mul(_WorldToViewMatrix, float4(positionWS, 1.0)).xyz;
    float4 positionCS = mul(_ViewToHClipMatrix, float4(positionVS, 1.0));
    float3 deltaPosVS = mul(_WorldToViewMatrix, float4(deltaPosWS, 1.0)).xyz;
    float4 deltaPosCS = mul(_ViewToHClipMatrix, float4(deltaPosVS, 1.0));
    positionCS /= abs(positionCS.w);
    deltaPosCS /= abs(deltaPosCS.w);

    float3 reflectCS = normalize(deltaPosCS - positionCS);
    
    float maxLoops = _MaxLoop;
    float stepSize = 1.0;
    float maxStepSize = _MaxStepSize;

    float thickness = _Thickness;

    float2 deltaUV = 0.0;
    float2 currUV = uv;

    UNITY_FLATTEN
    if (abs(reflectCS.x * _ScaledScreenParams.x) > abs(reflectCS.y * _ScaledScreenParams.y))
    {
        deltaUV = reflectCS.xy * rcp(abs(reflectCS.x) * _ScaledScreenParams.x);
    }
    else
    {
        deltaUV = reflectCS.xy * rcp(abs(reflectCS.y) * _ScaledScreenParams.y);
    }

    UNITY_LOOP
    for (int i = 0; i < maxLoops ; ++i)
    {
        stepSize = 1.0;

        currUV += deltaUV * stepSize;

        UNITY_BRANCH
        if (currUV.x < 0.0 || currUV.x > 1.0 || currUV.y < 0.0 || currUV.y > 1.0)
        {
            color = 0.0;
            return false;
        }
        
        float3 viewRayWS = normalize(GetViewRay(currUV));

        float3 intersectPosWS = 0.0;
        if (!IntersectPoint(intersectPosWS, GetCameraPositionWS(), viewRayWS, positionWS, reflectWS))
        {
            color = 0.0;
            return false;
        }

        float3 intersectPosVS = mul(_WorldToViewMatrix, float4(intersectPosWS, 1.0)).xyz;
        float4 intersectPosCS = mul(_ViewToHClipMatrix, float4(intersectPosVS, 1.0));
        float3 intersectPosNDC = intersectPosCS.xyz / intersectPosCS.w;
        intersectPosNDC.xy = intersectPosNDC.xy * 0.5 + 0.5;

        UNITY_BRANCH
        if (intersectPosNDC.z < -1.0 || intersectPosNDC.z > 1.0)
        {
            color = 0.0;
            return false;
        }

        float rayZ = intersectPosNDC.z;
        float mapZ = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, intersectPosNDC.xy);

        float rayDepth = LinearEyeDepth(rayZ, _ZBufferParams);
        float mapDepth = LinearEyeDepth(mapZ, _ZBufferParams);

        UNITY_BRANCH
        if (rayDepth > mapDepth && rayDepth < mapDepth + thickness)
        {
            color = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, intersectPosNDC.xy).rgb;
            return true;
        }
    }
    
    return false;
}

bool SampleReflectionColor(out half3 color, float2 uv, float3 normalWS, float3 rayDirWS)
{
    UNITY_BRANCH
    if (dot(normalWS, normalWS) < FLT_EPS)
    {
        return false;
    }

    float3 viewDirWS = normalize(rayDirWS);
    float3 reflectWS = reflect(viewDirWS, normalWS);
    
    float posZ = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    float depth = Linear01Depth(posZ, _ZBufferParams);
    float3 positionWS = GetCameraPositionWS() + depth * rayDirWS;
    float3 deltaPosWS = positionWS + reflectWS;
    float3 positionVS = mul(_WorldToViewMatrix, float4(positionWS, 1.0)).xyz;
    float4 positionCS = mul(_ViewToHClipMatrix, float4(positionVS, 1.0));
    float3 deltaPosVS = mul(_WorldToViewMatrix, float4(deltaPosWS, 1.0)).xyz;
    float4 deltaPosCS = mul(_ViewToHClipMatrix, float4(deltaPosVS, 1.0));
    positionCS /= abs(positionCS.w);
    deltaPosCS /= abs(deltaPosCS.w);

    float3 reflectCS = normalize(deltaPosCS - positionCS);
    
    float maxLoops = _MaxLoop;
    float stepSize = 1.0;
    float maxStepSize = _MaxStepSize;

    float thickness = _Thickness;

    float2 deltaUV = 0.0;
    float2 currUV = uv;
    
    UNITY_FLATTEN
    if (abs(reflectCS.x * _ScaledScreenParams.x) > abs(reflectCS.y * _ScaledScreenParams.y))
    {
        deltaUV = reflectCS.xy * rcp(abs(reflectCS.x) * _ScaledScreenParams.x);
    }
    else
    {
        deltaUV = reflectCS.xy * rcp(abs(reflectCS.y) * _ScaledScreenParams.y);
    }
    
    UNITY_LOOP
    for (int i = 0; i < maxLoops ; ++i)
    {
        stepSize = lerp(stepSize, stepSize * 0.5, step(maxStepSize, stepSize));

        currUV += deltaUV * stepSize;

        UNITY_BRANCH
        if (currUV.x < 0.0 || currUV.x > 1.0 || currUV.y < 0.0 || currUV.y > 1.0)
        {
            currUV -= deltaUV * stepSize;
            stepSize *= 0.5;

            UNITY_BRANCH
            if (stepSize < 1.0)
            {
                color = 0.0;
                return false;
            }
            continue;
        }
        
        float3 viewRayWS = normalize(GetViewRay(currUV));

        float3 intersectPosWS = 0.0;
        if (!IntersectPoint(intersectPosWS, GetCameraPositionWS(), viewRayWS, positionWS, reflectWS))
        {
            color = 0.0;
            return false;
        }

        float3 intersectPosVS = mul(_WorldToViewMatrix, float4(intersectPosWS, 1.0)).xyz;
        float4 intersectPosCS = mul(_ViewToHClipMatrix, float4(intersectPosVS, 1.0));
        float3 intersectPosNDC = intersectPosCS.xyz / intersectPosCS.w;
        intersectPosNDC.xy = intersectPosNDC.xy * 0.5 + 0.5;

        UNITY_BRANCH
        if (intersectPosNDC.z < -1.0 || intersectPosNDC.z > 1.0)
        {
            color = 0.0;
            return false;
        }

        float rayZ = intersectPosNDC.z;
        float mapZ = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, intersectPosNDC.xy);

        float rayDepth = LinearEyeDepth(rayZ, _ZBufferParams);
        float mapDepth = LinearEyeDepth(mapZ, _ZBufferParams);

        UNITY_BRANCH
        if (rayDepth > mapDepth && rayDepth < mapDepth + thickness)
        {
            UNITY_BRANCH
            if (stepSize <= 1.0)
            {
                color = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, intersectPosNDC.xy).rgb;
                return true;
            }
            
            currUV -= stepSize * deltaUV;
            stepSize *= 0.5;
        }
        else
        {
            stepSize *= 2.0;
        }
    }

    return false;
}

Varyings ScreenSpaceReflectionPassVertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.baseUV = input.baseUV;

    float depth = 1.0;
#if defined(UNITY_REVERSED_Z)
    depth = 0.0;
#endif

    // In order to recreate world position from depth map
    float3 positionWS = ComputeWorldSpacePosition(input.baseUV, depth, UNITY_MATRIX_I_VP);
    output.viewRay = positionWS - _WorldSpaceCameraPos.xyz;

    return output;
}

half4 ScreenSpaceReflectionPassFragment(Varyings input) : SV_Target
{
    float4 colorMap = SAMPLE_TEXTURE2D(_SourceTex, sampler_SourceTex, input.baseUV);
    float4 normalMap = SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, input.baseUV);
    //float3 normalWS = normalMap.xyz;// UnpackNormal(normalMap);
    float3 normalWS = normalize(UnpackNormal(normalMap.xyz));

    // colorMap = 0.0;

    half3 color;
    if (SampleReflectionColor(color, input.baseUV, normalWS, input.viewRay))
    {
        colorMap.rgb = colorMap.rgb * 0.9 + color * 0.1;
    }
    
    return half4(colorMap.rgb, 1.0);
}

#endif

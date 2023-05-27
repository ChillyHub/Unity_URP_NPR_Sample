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
    float3 normalWS = normalMap.xyz;// UnpackNormal(normalMap);
    // float3 normalWS = UnpackNormal(normalMap);

    // colorMap = 0.0;

    half3 color;
    if (SampleReflectionColor(color, input.baseUV, normalWS, input.viewRay))
    {
        colorMap.rgb = colorMap.rgb * 0.9 + color * 0.1;
    }
    
    return half4(colorMap.rgb, 1.0);
}

#endif

/*
// By Morgan McGuire and Michael Mara at Williams College 2014
// Released as open source under the BSD 2-Clause License
// http://opensource.org/licenses/BSD-2-Clause
#define point2 vec2
#define point3 vec3

float distanceSquared(vec2 a, vec2 b) { a -= b; return dot(a, a); }

// Returns true if the ray hit something
bool traceScreenSpaceRay1(
 // Camera-space ray origin, which must be within the view volume
 point3 csOrig, 

 // Unit length camera-space ray direction
 vec3 csDir,

 // A projection matrix that maps to pixel coordinates (not [-1, +1]
 // normalized device coordinates)
 mat4x4 proj, 

 // The camera-space Z buffer (all negative values)
 sampler2D csZBuffer,

 // Dimensions of csZBuffer
 vec2 csZBufferSize,

 // Camera space thickness to ascribe to each pixel in the depth buffer
 float zThickness, 

 // (Negative number)
 float nearPlaneZ, 

 // Step in horizontal or vertical pixels between samples. This is a float
 // because integer math is slow on GPUs, but should be set to an integer >= 1
 float stride,

 // Number between 0 and 1 for how far to bump the ray in stride units
 // to conceal banding artifacts
 float jitter,

 // Maximum number of iterations. Higher gives better images but may be slow
 const float maxSteps, 

 // Maximum camera-space distance to trace before returning a miss
 float maxDistance, 

 // Pixel coordinates of the first intersection with the scene
 out point2 hitPixel, 

 // Camera space location of the ray hit
 out point3 hitPoint) {

    // Clip to the near plane    
    float rayLength = ((csOrig.z + csDir.z * maxDistance) > nearPlaneZ) ?
        (nearPlaneZ - csOrig.z) / csDir.z : maxDistance;
    point3 csEndPoint = csOrig + csDir * rayLength;

    // Project into homogeneous clip space
    vec4 H0 = proj * vec4(csOrig, 1.0);
    vec4 H1 = proj * vec4(csEndPoint, 1.0);
    float k0 = 1.0 / H0.w, k1 = 1.0 / H1.w;

    // The interpolated homogeneous version of the camera-space points  
    point3 Q0 = csOrig * k0, Q1 = csEndPoint * k1;

    // Screen-space endpoints
    point2 P0 = H0.xy * k0, P1 = H1.xy * k1;

    // If the line is degenerate, make it cover at least one pixel
    // to avoid handling zero-pixel extent as a special case later
    P1 += vec2((distanceSquared(P0, P1) < 0.0001) ? 0.01 : 0.0);
    vec2 delta = P1 - P0;

    // Permute so that the primary iteration is in x to collapse
    // all quadrant-specific DDA cases later
    bool permute = false;
    if (abs(delta.x) < abs(delta.y)) { 
        // This is a more-vertical line
        permute = true; delta = delta.yx; P0 = P0.yx; P1 = P1.yx; 
    }

    float stepDir = sign(delta.x);
    float invdx = stepDir / delta.x;

    // Track the derivatives of Q and k
    vec3  dQ = (Q1 - Q0) * invdx;
    float dk = (k1 - k0) * invdx;
    vec2  dP = vec2(stepDir, delta.y * invdx);

    // Scale derivatives by the desired pixel stride and then
    // offset the starting values by the jitter fraction
    dP *= stride; dQ *= stride; dk *= stride;
    P0 += dP * jitter; Q0 += dQ * jitter; k0 += dk * jitter;

    // Slide P from P0 to P1, (now-homogeneous) Q from Q0 to Q1, k from k0 to k1
    point3 Q = Q0; 

    // Adjust end condition for iteration direction
    float  end = P1.x * stepDir;

    float k = k0, stepCount = 0.0, prevZMaxEstimate = csOrig.z;
    float rayZMin = prevZMaxEstimate, rayZMax = prevZMaxEstimate;
    float sceneZMax = rayZMax + 100;
    for (point2 P = P0; 
         ((P.x * stepDir) <= end) && (stepCount < maxSteps) &&
         ((rayZMax < sceneZMax - zThickness) || (rayZMin > sceneZMax)) &&
          (sceneZMax != 0); 
         P += dP, Q.z += dQ.z, k += dk, ++stepCount) {
        
        rayZMin = prevZMaxEstimate;
        rayZMax = (dQ.z * 0.5 + Q.z) / (dk * 0.5 + k);
        prevZMaxEstimate = rayZMax;
        if (rayZMin > rayZMax) { 
           float t = rayZMin; rayZMin = rayZMax; rayZMax = t;
        }

        hitPixel = permute ? P.yx : P;
        // You may need hitPixel.y = csZBufferSize.y - hitPixel.y; here if your vertical axis
        // is different than ours in screen space
        sceneZMax = texelFetch(csZBuffer, int2(hitPixel), 0);
    }
    
    // Advance Q based on the number of steps
    Q.xy += dQ.xy * stepCount;
    hitPoint = Q * (1.0 / k);
    return (rayZMax >= sceneZMax - zThickness) && (rayZMin < sceneZMax);
}
*/

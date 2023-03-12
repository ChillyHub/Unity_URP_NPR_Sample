#ifndef ATMOSPHERE_SCATTERING_INCLUDED
#define ATMOSPHERE_SCATTERING_INCLUDED

#include "Assets/Shaders/ShaderLibrary/Utility.hlsl"

static const float sRefractive = 1.00029;
static const float sAtmosphereDensity = 2.504e25;

static const float3 sWaveLengths = float3(680.0, 550.0, 440.0) * 1e-9;  // nm to m
static const float3 sRayleighScatteringCoefficients =
    float3(0.00000519673, 0.0000121427, 0.0000296453);
static const float sMieScatteringCoefficient = 0.0001;
static const float3 sMieScatteringCoefficients =
    float3(0.0001, 0.0001, 0.0001);

struct ScatteringData
{
    float3 lightColor;
    float3 rayleighSC;
    float3 mieSC;
    float radius;
    float thickness;
    float scale;
    float exposure;
    float gMie;
    float rayleighFac;
    float mieFac;
    float sampleCounts;
};

struct PathData
{
    float3 startPos;
    float3 endPos;
    float3 pathDir;
    float pathLength;
};

float3 TransformWorldToSpecial(float3 pos)
{
    return float3(0.0, pos.y, 0.0);
}

float3 TransformWorldToSpecial(float3 pos, float3 delta)
{
    return pos + delta;
}

/**
 * \brief 
 * \param tIn In sphere intersection point t
 * \param tOut Out sphere intersection point t
 * \param D Ray direction
 * \param O Ray original
 * \param C Sphere center
 * \param R Sphere radius
 */
void GetSphereIntersection(out float tIn, out float tOut, float3 D, float3 O, float3 C, float R)
{
    float3 co = O - C;
    float a = dot(D, D);
    float b = 2.0 * dot(D, co);
    float c = dot(co, co) - R * R;
    float m = sqrt(max(b * b - 4.0 * a * c, 0.0));
    float div = rcp(2.0 * a);

    tIn = (-b - m) * div;
    tOut = (-b + m) * div;
}

PathData GetLightPathData(ScatteringData data, float3 objPos, float3 lightDir)
{
    // Intersecting the atmosphere
    float tAIn, tAOut, tGIn, tGOut;
    GetSphereIntersection(tAIn, tAOut, lightDir, objPos, float3(0.0, -data.radius, 0.0), data.radius + data.thickness);
    GetSphereIntersection(tGIn, tGOut, lightDir, objPos, float3(0.0, -data.radius, 0.0), data.radius);
    float tStart = 0.0;
    float tEnd = lerp(tAOut, 0.0, step(0.0, tGIn) * step(HALF_EPS, tGOut - tGIn));

    PathData o;
    o.startPos = objPos + lightDir * tEnd;
    o.endPos = objPos + lightDir * tStart;
    o.pathDir = -lightDir;
    o.pathLength = tEnd - tStart;
    return o;
}

PathData GetViwePathData(ScatteringData data, float3 viewPos, float3 viewDir)
{
    // Intersecting the atmosphere
    float tAIn, tAOut, tGIn, tGOut;
    GetSphereIntersection(tAIn, tAOut, -viewDir, viewPos, float3(0.0, -data.radius, 0.0), data.radius + data.thickness);
    GetSphereIntersection(tGIn, tGOut, -viewDir, viewPos, float3(0.0, -data.radius, 0.0), data.radius);
    float tStart = lerp(max(tAIn, 0.0), tGOut, step(0.0, tGOut) * step(tGIn, 0.0));
    float tEnd = lerp(max(tAOut, 0.0), tGIn, step(0.0, tGIn) * step(HALF_EPS, tGOut - tGIn));

    PathData o;
    o.startPos = viewPos - viewDir * tEnd;
    o.endPos = viewPos - viewDir * tStart;
    o.pathDir = viewDir;
    o.pathLength = tEnd - tStart;
    return o;
}

// Without density ratio
float GetMieScatteringCoefficient()
{
    return sMieScatteringCoefficient;
    // 
    float var1 = sRefractive * sRefractive - 1.0;
    return 8.0 * PI * PI * PI * var1 * var1 * rcp(3.0 * sAtmosphereDensity);
}

// Without density ratio
float3 GetMieScatteringCoefficients()
{
    return sMieScatteringCoefficients;
    // 
    float var1 = sRefractive * sRefractive - 1.0;
    return 8.0 * PI * PI * PI * var1 * var1 * rcp(3.0 * sAtmosphereDensity);
}

// Without density ratio
float GetRayleighScatteringCoefficient(float waveLength)
{
    float var1 = sRefractive * sRefractive - 1.0;
    float var2 = waveLength * waveLength;
    var2 = var2 * var2;
    return 8.0 * PI * PI * PI * var1 * var1 * rcp(3.0 * sAtmosphereDensity * var2);
}

// Without density ratio
float3 GetRayleighScatteringCoefficients(float3 waveLengths)
{
    return sRayleighScatteringCoefficients;
    // 
    float var1 = sRefractive * sRefractive - 1.0;
    float3 var2 = waveLengths * waveLengths;
    var2 = var2 * var2;
    return 8.0 * PI * PI * PI * var1 * var1 * rcp(3.0 * sAtmosphereDensity * var2);
}

// Without density ratio
float3 GetRayleighScatteringCoefficients(float3 waveLengths, float3 mieSC)
{
    return sRayleighScatteringCoefficients;
    // 
    float3 var2 = waveLengths * waveLengths;
    return mieSC * rcp(var2 * var2);
}

ScatteringData GetScatteringData(float3 lightColor, float radius, float thickness,
    float scale, float exposure, float gMie, float rayleighFac,  float mieFac, float sampleCounts)
{
    ScatteringData o;
    o.lightColor = lightColor;
    o.mieSC = GetMieScatteringCoefficients();
    o.rayleighSC = GetRayleighScatteringCoefficients(sWaveLengths, o.mieSC);
    o.radius = radius;
    o.thickness = thickness;
    o.scale = scale;
    o.exposure = exposure * 20.0;
    o.gMie = gMie;
    o.rayleighFac = rayleighFac;
    o.mieFac = mieFac;
    o.sampleCounts = sampleCounts;

    return o;
}

void CalculateScatteringData(inout ScatteringData o)
{
    o.mieSC = GetMieScatteringCoefficients();
    o.rayleighSC = GetRayleighScatteringCoefficients(sWaveLengths, o.mieSC);
    o.exposure = o.exposure * 20.0;
}

float GetDensityRatio(float height, float thickness)
{
    return exp(-height * rcp(thickness));
}

float GetRayleighPhaseFunction(float cosTheta)
{
    return 3.0 * (1.0 + cosTheta * cosTheta) * rcp(16.0 * PI);
}

float GetMiePhaseFunction(float cosTheta, float g)
{
    float g2 = g * g;
    float cos2 = cosTheta * cosTheta;
    float num = 3.0 * (1.0 - g2) * (1.0 + cos2);
    float denom = rcp(8.0 * PI * (2.0 + g2) * pow(abs(1.0 + g2 - 2.0 * g * cosTheta), 1.5));
    return num * denom;
}

float GetOpticalDepth(ScatteringData scaData, PathData pathData)
{
    float opticalDepth = 0.0;
    float distanceStep = pathData.pathLength * rcp(scaData.sampleCounts);

    float3 currPos = pathData.startPos + pathData.pathDir * distanceStep * 0.5;

    UNITY_LOOP
    for (int i = 0; i < scaData.sampleCounts; ++i)
    {
        float height = length(currPos - float3(0.0, -scaData.radius, 0.0)) - scaData.radius;
        
        opticalDepth += GetDensityRatio(height, scaData.thickness) * distanceStep * scaData.scale;

        currPos += distanceStep * pathData.pathDir;
    }

    return opticalDepth;
}

float GetLightOpticalDepthLUT(TEXTURE2D(lut), SAMPLER(samplerLUT), ScatteringData scaData, PathData pathData)
{
    float3 up = pathData.endPos - float3(0.0, -scaData.radius, 0.0);
    float3 dir = -pathData.pathDir;
    float height = length(up) - scaData.radius;
    float cosTheta = dot(normalize(up), dir);
    float2 uv = float2(height * rcp(scaData.thickness), cosTheta * 0.5 + 0.5);

    return SAMPLE_TEXTURE2D_LOD(lut, samplerLUT, uv, 0.0).r;
}

float GetViewOpticalDepthLUT(TEXTURE2D(lut), SAMPLER(samplerLUT), ScatteringData scaData, PathData pathData)
{
    float3 up = pathData.startPos - float3(0.0, -scaData.radius, 0.0);
    float3 dir = pathData.pathDir;
    float height = length(up) - scaData.radius;
    float cosTheta = dot(normalize(up), dir);                       
    float2 uv = float2(height * rcp(scaData.thickness), cosTheta * 0.5 + 0.5);

    return SAMPLE_TEXTURE2D_LOD(lut, samplerLUT, uv, 0.0).r;
}

float2 GetOpticalDepths(ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetOpticalDepth(scaData, lightData);
    float depthBC = GetOpticalDepth(scaData, viewData);
    return float2(depthAB, depthBC);
}

float2 GetOpticalDepthsLUT(TEXTURE2D(lut), SAMPLER(samplerLUT),
    ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetLightOpticalDepthLUT(lut, samplerLUT, scaData, lightData);
    float depthBC = GetViewOpticalDepthLUT(lut, samplerLUT, scaData, viewData);
    return float2(depthAB, depthBC);
}

float3 GetRayleighTransmittances(ScatteringData scaData, PathData pathData)
{
    return exp(-scaData.rayleighSC * GetOpticalDepth(scaData, pathData));
}

float3 GetRayleighTotalTransmittances(ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetOpticalDepth(scaData, lightData);
    float depthBC = GetOpticalDepth(scaData, viewData);
    return exp(-scaData.rayleighSC * (depthAB + depthBC));
}

float3 GetRayleighTotalTransmittances(ScatteringData scaData, float2 depths)
{
    return exp(-scaData.rayleighSC * (depths.x + depths.y));
}

float3 GetRayleighTotalTransmittancesLUT(TEXTURE2D(lut), SAMPLER(samplerLUT),
    ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetLightOpticalDepthLUT(lut, samplerLUT, scaData, lightData);
    float depthBC = GetViewOpticalDepthLUT(lut, samplerLUT, scaData, viewData);
    return exp(-scaData.rayleighSC * (depthAB + depthBC));
}

float3 GetMieTransmittances(ScatteringData scaData, PathData pathData)
{
    return exp(-scaData.mieSC * GetOpticalDepth(scaData, pathData));
}

float3 GetMieTotalTransmittances(ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetOpticalDepth(scaData, lightData);
    float depthBC = GetOpticalDepth(scaData, viewData);
    return exp(-scaData.mieSC * (depthAB + depthBC));
}

float3 GetMieTotalTransmittances(ScatteringData scaData, float2 depths)
{
    return exp(-scaData.mieSC * (depths.x + depths.y));
}

float3 GetMieTotalTransmittancesLUT(TEXTURE2D(lut), SAMPLER(samplerLUT),
    ScatteringData scaData, PathData lightData, PathData viewData)
{
    float depthAB = GetLightOpticalDepthLUT(lut, samplerLUT, scaData, lightData);
    float depthBC = GetViewOpticalDepthLUT(lut, samplerLUT, scaData, viewData);
    return exp(-scaData.mieSC * (depthAB + depthBC));
}

float3 GetInscatteringColor(ScatteringData scaData, float3 lightDir, float3 viewDir, float3 viewPosWS)
{
    float3 viewPos = TransformWorldToSpecial(viewPosWS);

    PathData viewPathData = GetViwePathData(scaData, viewPos, viewDir);

    UNITY_BRANCH
    if (viewPathData.pathLength < FLT_EPS)
    {
        return 0.0;
    }

    float3 inscatterRay = 0.0;
    float3 inscatterMie = 0.0;
    float stepLength = viewPathData.pathLength * rcp(scaData.sampleCounts);
    
    float3 currPos = viewPathData.startPos + viewPathData.pathDir * stepLength * 0.5;
    viewPathData.pathLength -= stepLength * 0.5;

    UNITY_LOOP
    for (int i = 0; i < scaData.sampleCounts; ++i)
    {
        float height = length(currPos - float3(0.0, -scaData.radius, 0.0)) - scaData.radius;

        PathData lightPathData = GetLightPathData(scaData, currPos, lightDir);

        UNITY_BRANCH
        if (lightPathData.pathLength < FLT_EPS)
        {
            currPos += stepLength * viewPathData.pathDir;
            continue;
        }

        float ratio = GetDensityRatio(height, scaData.thickness);
        float2 depths = GetOpticalDepths(scaData, lightPathData, viewPathData);
        float3 transRay = GetRayleighTotalTransmittances(scaData, depths);
        float3 transMie = GetMieTotalTransmittances(scaData, depths);
        inscatterRay += transRay * ratio * stepLength * scaData.scale;
        inscatterMie += transMie * ratio * stepLength * scaData.scale;
        
        currPos += stepLength * viewPathData.pathDir;
        viewPathData.pathLength -= stepLength;
    }

    float cosTheta = dot(-lightDir, viewDir);
    float3 resultRay = scaData.rayleighFac * scaData.rayleighSC * GetRayleighPhaseFunction(cosTheta) * inscatterRay;
    float3 reslutMie = scaData.mieFac * scaData.mieSC * GetMiePhaseFunction(cosTheta, scaData.gMie) * inscatterMie;
    return scaData.lightColor * (resultRay + reslutMie) * scaData.exposure;
}

float3 GetInscatteringColorLUT(TEXTURE2D(LUT), SAMPLER(samplerLUT),  
    ScatteringData scaData, float3 lightDir, float3 viewDir, float3 viewPosWS)
{
    float3 viewPos = TransformWorldToSpecial(viewPosWS);

    PathData viewPathData = GetViwePathData(scaData, viewPos, viewDir);

    UNITY_BRANCH
    if (viewPathData.pathLength < FLT_EPS)
    {
        return 0.0;
    }

    float3 inscatterRay = 0.0;
    float3 inscatterMie = 0.0;
    float stepLength = viewPathData.pathLength * rcp(scaData.sampleCounts);
    
    float3 currPos = viewPathData.startPos + viewPathData.pathDir * stepLength * 0.5;
    viewPathData.pathLength -= stepLength * 0.5;

    UNITY_LOOP
    for (int i = 0; i < scaData.sampleCounts; ++i)
    {
        float height = length(currPos - float3(0.0, -scaData.radius, 0.0)) - scaData.radius;

        PathData lightPathData = GetLightPathData(scaData, currPos, lightDir);

        UNITY_BRANCH
        if (lightPathData.pathLength < FLT_EPS)
        {
            currPos += stepLength * viewPathData.pathDir;
            continue;
        }

        float2 uv = float2(height * rcp(scaData.thickness), 0.5);
        
        float ratio = SAMPLE_TEXTURE2D_LOD(LUT, samplerLUT, uv, 0.0).g;
        float2 depths = GetOpticalDepthsLUT(LUT, samplerLUT, scaData, lightPathData, viewPathData);
        float3 transRay = GetRayleighTotalTransmittances(scaData, depths);
        float3 transMie = GetMieTotalTransmittances(scaData, depths);
        inscatterRay += transRay * ratio * stepLength * scaData.scale;
        inscatterMie += transMie * ratio * stepLength * scaData.scale;
        
        currPos += stepLength * viewPathData.pathDir;
        viewPathData.pathLength -= stepLength;
    }

    float cosTheta = dot(-lightDir, viewDir);
    float3 resultRay = scaData.rayleighFac * scaData.rayleighSC * GetRayleighPhaseFunction(cosTheta) * inscatterRay;
    float3 reslutMie = scaData.mieFac * scaData.mieSC * GetMiePhaseFunction(cosTheta, scaData.gMie) * inscatterMie;
    return scaData.lightColor * (resultRay + reslutMie) * scaData.exposure;
}

float3 GetInscatteringColorSimple(float3 lightColor, float3 scatterCoefs, float phase, float len)
{
    return lightColor * phase * (1.0 - exp(-scatterCoefs * len));
}

float3 GetInscatteringColorSimple(float3 lightColor, float3 raySC, float3 mieSC,
    float rayPhase, float miePhase, float len, float rayFac = 1.0, float mieFac = 1.0)
{
    raySC *= rayFac;
    mieSC *= mieFac;
    float3 SCP = (raySC * rayPhase + mieSC * miePhase) * rcp(raySC + mieSC);
    return lightColor * SCP * (1.0 - exp(-(raySC + mieSC) * len));
}

float3 GetInscatteringColorVolume(ScatteringData scaData, float3 lightDir, float3 viewDir,
    float3 viewPosWS, float3 positionWS)
{
    float realLen = length(positionWS - viewPosWS);
    float stepRealLength = realLen * rcp(scaData.sampleCounts);
    float3 currRealPos = viewPosWS - viewDir * stepRealLength * 0.5;

    float3 viewPos = TransformWorldToSpecial(viewPosWS);
    float3 position = TransformWorldToSpecial(positionWS, viewPos - viewPosWS);

    PathData viewPathData;
    viewPathData.startPos = viewPos;
    viewPathData.endPos = position;
    viewPathData.pathDir = -viewDir;
    viewPathData.pathLength = realLen;

    UNITY_BRANCH
    if (viewPathData.pathLength < FLT_EPS)
    {
        return 0.0;
    }

    float3 inscatterRay = 0.0;
    float3 inscatterMie = 0.0;
    float stepLength = viewPathData.pathLength * rcp(scaData.sampleCounts);
    
    float3 currPos = viewPathData.startPos + viewPathData.pathDir * stepLength * 0.5;
    viewPathData.pathLength -= stepLength * 0.5;

    UNITY_LOOP
    for (int i = 0; i < scaData.sampleCounts; ++i)
    {
        float4 shadowCoord = TransformWorldToShadowCoord(currRealPos);
        float shadow = MainLightRealtimeShadow(shadowCoord);

        UNITY_BRANCH
        if (shadow < 0.1)
        {
            currRealPos += stepRealLength * viewPathData.pathDir;
            currPos += stepLength * viewPathData.pathDir;
            continue;
        }
        
        float height = length(currPos - float3(0.0, -scaData.radius, 0.0)) - scaData.radius;

        PathData lightPathData = GetLightPathData(scaData, currPos, lightDir);

        UNITY_BRANCH
        if (lightPathData.pathLength < FLT_EPS)
        {
            currRealPos += stepRealLength * viewPathData.pathDir;
            currPos += stepLength * viewPathData.pathDir;
            continue;
        }

        float ratio = GetDensityRatio(height, scaData.thickness);
        float2 depths = GetOpticalDepths(scaData, lightPathData, viewPathData);
        float3 transRay = GetRayleighTotalTransmittances(scaData, depths);
        float3 transMie = GetMieTotalTransmittances(scaData, depths);
        inscatterRay += transRay * ratio * stepLength * scaData.scale * shadow;
        inscatterMie += transMie * ratio * stepLength * scaData.scale * shadow;

        currRealPos += stepRealLength * viewPathData.pathDir;
        currPos += stepLength * viewPathData.pathDir;
        viewPathData.pathLength -= stepLength;
    }

    float cosTheta = dot(-lightDir, viewDir);
    float3 resultRay = scaData.rayleighFac * scaData.rayleighSC * GetRayleighPhaseFunction(cosTheta) * inscatterRay;
    float3 reslutMie = scaData.mieFac * scaData.mieSC * GetMiePhaseFunction(cosTheta, scaData.gMie) * inscatterMie;
    return scaData.lightColor * (resultRay + reslutMie) * scaData.exposure;
}

float3 GetSunRenderColor(float3 sunColor, float3 lightDir, float3 viewDir, float gSun)
{
    float visual = smoothstep(-0.01, 0.0, dot(viewDir, float3(0.0, -1.0, 0.0)));
    return sunColor * GetMiePhaseFunction(dot(-lightDir, viewDir), gSun) * visual;
}

void CalculateAerialPerspectiveSimple(inout float3 color,
    float3 lightColor, float3 positionWS, float3 viewPos, float3 viewDir, float3 lightDir,
    float gMie = 0.0, float mieFac = 0.0, float rayFac = 1.0)
{
    float len = length(positionWS - viewPos);
    float cosTheta = dot(viewDir, -lightDir);
    float phaseRay = GetRayleighPhaseFunction(cosTheta);
    float phaseMie = GetMiePhaseFunction(cosTheta, gMie);
    float3 scatteringMie = GetMieScatteringCoefficients();
    float3 scatteringRay = GetRayleighScatteringCoefficients(sWaveLengths, scatteringMie);

    float3 excintion = rayFac* exp(-scatteringRay * len) + mieFac * exp(-scatteringMie * len);

    float3 inscatter = GetInscatteringColorSimple(
        lightColor, scatteringRay, scatteringMie, phaseRay, phaseMie, len, rayFac, mieFac);

    color = color * excintion + inscatter;
}

void CalculateAerialPerspective(inout float3 color, ScatteringData scaData,
    float3 lightDir, float3 viewDir, float3 viewPosWS, float3 positionWS)
{
    float len = length(positionWS - viewPosWS);
    float3 scatteringMie = GetMieScatteringCoefficients();
    float3 scatteringRay = GetRayleighScatteringCoefficients(sWaveLengths);

    float3 excintion = scaData.rayleighFac * exp(-scatteringRay * len) +
        scaData.mieFac * exp(-scatteringMie * len);

    float3 inscatter = GetInscatteringColorVolume(scaData, lightDir, viewDir, viewPosWS, positionWS);

    color = color * excintion + inscatter;
}

#endif
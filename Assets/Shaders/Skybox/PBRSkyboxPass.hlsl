#ifndef PBR_SKYBOX_PASS_INCLUDED
#define PBR_SKYBOX_PASS_INCLUDED

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 sunLightDir : TEXCOORD1;
    float3 sunLightColor : TEXCOORD2;
    float3 viewPosWS : TEXCOORD3;
    float3 viewDir : TEXCOORD4;
    float3 color : TEXCOORD5;
    
    UNITY_VERTEX_OUTPUT_STEREO
};

CBUFFER_START(UnityPerMaterial)
    float4 _SkyboxColor;
    float _Thickness;
    float _Radius;
    float _RScale;
    float _SampleCounts;
    float _Exposure;

    float3 _ColorRed;
    float3 _ColorGreen;
    float3 _ColorBlue;
    float3 _NightColor;
    float3 _SunColor;

    float _gMie;
    float _mieFac;
CBUFFER_END

static const float sRefractive = 1.00029;
static const float sAtmosphereDensity = 2.504e25;

static const float3 sWaveLengths = float3(680.0, 550.0, 440.0) * 1e-9;
static const float3 sRayleight = float3(2.969e-5, 1.223e-5, 5.23e-6);

#define RADIUS (_Radius * pow(_RScale, 3))       // 
#define THICKNESS (_Thickness * pow(_RScale, 3)) //
#define EXPOSURE (_Exposure * 20.0)              //

#define LENGTH_KM_TO_M(len) (len * 1000.0) // km to m
#define LENGTH_M_TO_KM(len) (len * 0.001)  // km to m

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

float GetAtmosphereLightRange(out float3 lightStart, out float3 lightEnd, float3 objPos, float3 lightDir)
{
    // Intersecting the atmosphere
    float tAIn, tAOut, tGIn, tGOut;
    GetSphereIntersection(tAIn, tAOut, lightDir, objPos, float3(0.0, -RADIUS, 0.0), RADIUS + THICKNESS);
    GetSphereIntersection(tGIn, tGOut, lightDir, objPos, float3(0.0, -RADIUS, 0.0), RADIUS);
    float tStart = 0.0;
    float tEnd = lerp(tAOut, 0.0, step(0.0, tGIn) * step(HALF_EPS, tGOut - tGIn));
    
    lightStart = objPos + lightDir * tEnd;
    lightEnd = objPos + lightDir * tStart;

    return step(FLT_EPS, tEnd - tStart);
}

float GetAtmosphereViewRange(out float3 viewStart, out float3 viewEnd, float3 viewPos, float3 viewDir)
{
    viewPos.x = 0.0;
    viewPos.z = 0.0;
    viewPos.y = LENGTH_M_TO_KM(viewPos.y);

    // Intersecting the atmosphere
    float tAIn, tAOut, tGIn, tGOut;
    GetSphereIntersection(tAIn, tAOut, -viewDir, viewPos, float3(0.0, -RADIUS, 0.0), RADIUS + THICKNESS);
    GetSphereIntersection(tGIn, tGOut, -viewDir, viewPos, float3(0.0, -RADIUS, 0.0), RADIUS);
    float tStart = lerp(max(tAIn, 0.0), tGOut, step(0.0, tGOut) * step(tGIn, 0.0));
    float tEnd = lerp(max(tAOut, 0.0), tGIn, step(0.0, tGIn) * step(HALF_EPS, tGOut - tGIn));

    viewStart = viewPos - viewDir * tEnd;
    viewEnd = viewPos - viewDir * tStart;

    return step(FLT_EPS, tEnd - tStart);
}

// Without density ratio
float GetScatteringCoefficient(float waveLength)
{
    //return waveLength;
    float var1 = sRefractive * sRefractive - 1.0;
    float var2 = waveLength * waveLength;
    var2 = var2 * var2;
    return 8.0 * PI * PI * PI * var1 * var1 * rcp(3.0 * sAtmosphereDensity * var2);
}

float GetDensityRatio(float height)
{
    return exp(-height * rcp(THICKNESS));
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
    float denom = rcp(8.0 * PI * (2.0 + g2) * pow(1.0 + g2 - 2.0 * g * cosTheta, 1.5));
    return num * denom;
}

float GetTotalPhaseFunction(float cosTheta, float mieFac)
{
    float a = GetRayleighPhaseFunction(cosTheta);
    float b = GetMiePhaseFunction(cosTheta, _gMie);
    return lerp(a, b, mieFac);
}

float GetOpticalDepth(inout float isOcclusion, float3 startPos, float distanceAB, float3 dirAB)
{
    float opticalDepth = 0.0;
    float distanceStep = distanceAB * rcp(_SampleCounts);

    float3 currPos = startPos + dirAB * distanceStep * 0.5;

    UNITY_LOOP
    for (int i = 0; i < _SampleCounts; ++i)
    {
        float height = length(currPos - float3(0.0, -RADIUS, 0.0)) - RADIUS;

        UNITY_BRANCH
        if (height < 0.0)
        {
            isOcclusion = 1.0;
            return 0.0;
        }
        
        opticalDepth += GetDensityRatio(height) * LENGTH_KM_TO_M(distanceStep);

        currPos += distanceStep * dirAB;
    }

    return opticalDepth;
}

float GetTransmittance(float scatteringCoefficient, float3 startPos, float distanceAB, float3 dirAB)
{
    float isOcclusion = 0.0;
    return exp(-scatteringCoefficient * GetOpticalDepth(isOcclusion, startPos, distanceAB, dirAB));
}

float GetTotalTransmittance(float scatteringCoefficient, float3 startPosA, float3 startPosB,
    float distanceAB, float distanceBC, float3 dirAB, float3 dirBC)
{
    float isOcclusion = 0.0;
    float depthAB = GetOpticalDepth(isOcclusion, startPosA, distanceAB, dirAB);
    float depthBC = GetOpticalDepth(isOcclusion, startPosB, distanceBC, dirBC);
    return lerp(exp(-scatteringCoefficient * (depthAB + depthBC)), 0.0, step(0.5, isOcclusion));
}

float GetInscattering(float inWaveIntensity, float waveLength, float cosTheta,
    float3 lightDir, float3 viewDir, float3 viewPos)
{
    float3 viewStart, viewEnd;

    UNITY_BRANCH
    if (GetAtmosphereViewRange(viewStart, viewEnd, viewPos, viewDir) < 0.5)
    {
        return 0.0;
    }

    float inscattering = 0.0;
    float stepLength = length(viewEnd - viewStart) * rcp(_SampleCounts);
    float scatteringCoefficient = GetScatteringCoefficient(waveLength);
    
    float3 currPos = viewStart + viewDir * stepLength * 0.5;;

    UNITY_LOOP
    for (int i = 0; i < _SampleCounts; ++i)
    {
        float transmittance = 1.0;
        float height = length(currPos - float3(0.0, -RADIUS, 0.0)) - RADIUS;

        float3 lightStart, lightEnd;

        UNITY_BRANCH
        if (GetAtmosphereLightRange(lightStart, lightEnd, currPos, lightDir) < 0.5)
        {
            currPos += stepLength * viewDir;
            continue;
        }

        float lengthLight = length(lightEnd - lightStart);
        float lengthView = (i + 1) * stepLength;
        transmittance *= GetTotalTransmittance(
            scatteringCoefficient, lightStart, currPos, lengthLight, lengthView, -lightDir, viewDir);
        transmittance *= GetDensityRatio(height);
        inscattering += transmittance * LENGTH_KM_TO_M(stepLength);

        currPos += stepLength * viewDir;
    }
    
    return inWaveIntensity * scatteringCoefficient * GetTotalPhaseFunction(cosTheta, _mieFac) * inscattering * EXPOSURE;
}

float3 GetInscatteringColor(float3 lightColor, float3 lightDir, float3 viewDir, float3 viewPos)
{
    float3 color = float3(0.0, 0.0, 0.0);
    float cosTheta = dot(-lightDir, viewDir);
    color += _ColorRed * GetInscattering(lightColor.r, sWaveLengths.r, cosTheta, lightDir, viewDir, viewPos);
    color += _ColorGreen * GetInscattering(lightColor.g, sWaveLengths.g, cosTheta, lightDir, viewDir, viewPos);
    color += _ColorBlue * GetInscattering(lightColor.b, sWaveLengths.b, cosTheta, lightDir, viewDir, viewPos);

    return color;
}

float GetSunRenderColor(float3 sunColor, float3 lightDir, float3 viewDir)
{
    return sunColor * GetMiePhaseFunction(dot(-lightDir, viewDir), 0.9999);
}

Varyings PBRSkyboxPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
    output.positionWS = TransformObjectToWorld(input.positionOS.xyz);

    Light light = GetMainLight();
    output.sunLightDir = light.direction;
    output.sunLightColor = light.color;

    output.viewPosWS = GetCameraPositionWS();
    output.viewDir = normalize(output.viewPosWS - output.positionWS);

    float3 inscatter = GetInscatteringColor(light.color, light.direction, output.viewDir, output.viewPosWS);
    float inscatterFac = smoothstep(0.00, 0.1, length(inscatter));
    output.color = lerp(_NightColor, inscatter, inscatterFac);

    return output;
}

float4 PBRSkyboxPassFragment(Varyings input) : SV_Target
{
    Light light = GetMainLight();
    float3 viewDir = normalize(input.viewPosWS - input.positionWS);
    return float4(input.color + GetSunRenderColor(_SunColor, light.direction, viewDir), 1.0);
    // Light light = GetMainLight();
    // float3 viewDir = normalize(input.viewPosWS - input.positionWS);
    // return float4(GetInscatteringColor(light.color, light.direction, viewDir, input.viewPosWS), 1.0);
}

#endif
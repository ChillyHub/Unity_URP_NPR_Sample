Shader "Custom/Skybox/PBRSkyboxLUT"
{
	Properties
	{
		[Foldout(Color Setting)]
    	[HDR] _LightColor("Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	[HDR] _SunColor("Sun Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	[HDR] _DayBlueColor("Day Blue Color", Color) = (0.0, 0.0, 1.0, 1.0)
    	[HDR] _DayGreenColor("Day Green Color", Color) = (0.0, 1.0, 0.0, 1.0)
    	[HDR] _DayRedColor("Day Red Color", Color) = (1.0, 0.0, 0.0, 1.0)
    	[HDR] _NightColor("Night Color", Color) = (0.0, 0.0, 0.0, 1.0)
    	_Exposure("Exposure", Range(0.0, 2.0)) = 1.0
		[FoldEnd]
	
    	[Foldout(Geometry Setting)]
    	_Thickness("Atmosphere Thickness (km)", Range(0.0, 1000.0)) = 160.0
    	_Radius("Planet Radius (km)", Float) = 6400.0
    	_RScale("Radius And Thicknesss Scale", Range(0.0, 10.0)) = 1.0
		[FoldEnd]
	
    	[Foldout(Physic Setting)]
    	_rayleighFac("Rayleigh Fac", Range(0.0, 1.0)) = 1.0
    	_mieFac("Mie Fac", Range(0.0, 1.0)) = 0.01
    	_gMie("Mie Coefficient g", Range(0.75, 0.9999)) = 0.1
    	_gSun("Sun Mie Coefficient g", Range(0.999, 1.0)) = 0.9999
		[FoldEnd]
	
    	[Foldout(Performance Setting)]
    	[IntRange] _SampleCounts("Sample Counts", Range(0, 60)) = 30
		[FoldEnd][HideInInspector] __("", Float) = 0.0
	}
	SubShader
	{
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		
		Cull Off
		ZWrite Off
		
		HLSLINCLUDE
        #include "Assets/Shaders/ShaderLibrary/Utility.hlsl"
        ENDHLSL

		Pass
		{
			HLSLPROGRAM

			#pragma target 4.5
			
			#pragma vertex PBRSkyboxPassVertex
			#pragma fragment PBRSkyboxPassFragment

			#include "Assets/Shaders/ShaderLibrary/AtmosphereScattering.hlsl"
			#include "Assets/Shaders/Skybox/PBRSkyboxLUTPass.hlsl"
			
			ENDHLSL
		}
	}
}

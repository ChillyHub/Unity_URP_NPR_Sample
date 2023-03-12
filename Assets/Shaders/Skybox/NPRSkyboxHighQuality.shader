Shader "Custom/Skybox/NPRSkyboxHighQuality"
{
	Properties
	{
		[Foldout(Color Setting)]
		[HDR] _ColorLight("Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
		[HDR] _ColorDayBright("Day Bright Color", Color) = (0.0, 0.0, 1.0, 1.0)
    	[HDR] _ColorDayMiddle("Day Middle Color", Color) = (0.0, 1.0, 0.0, 1.0)
    	[HDR] _ColorDayDark("Day Dark Color", Color) = (1.0, 0.0, 0.0, 1.0)
    	[HDR] _ColorNight("Night Color", Color) = (0.0, 0.0, 0.0, 1.0)
    	[HDR] _ColorSun("Sun Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	[HDR] _ColorMoon("Moon Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_Exposure ("Exposure", Range(0.0, 2.0)) = 1.0
		[FoldEnd]
		
		[Foldout(Geometry Setting)]
		_Thickness ("Atmosphere Thickness (km)", Range(0.0, 1000.0)) = 160.0
		_Radius ("Planet Radius (km)", Float) = 6400.0
		_RScale ("Radius And Thicknesss Scale", Range(0.0, 10.0)) = 1.0
		//[FoldEnd]
    	
		//[Foldout(Physic Setting)]
    	_gMie ("Mie Coefficient g", Range(-0.2, 0.2)) = 0.1
		_mieFac ("Mie Fac", Range(0.0, 1.0)) = 0.2
		_gSun ("Sun's Coefficient g", Range(0.999, 1.0)) = 0.9999
		//[FoldEnd]
    	
		//[Foldout(Perfomance Setting)]
		[IntRange] _SampleCounts ("Sample Counts", Range(0, 60)) = 30
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
			
			#pragma vertex NPRSkyboxHighQualityPassVertex
			#pragma fragment NPRSkyboxHighQualityPassFragment
			
			#include "Assets/Shaders/Skybox/NPRSkyboxHighQualityPass.hlsl"
			
			ENDHLSL
		}
	}
	CustomEditor "FoldoutBaseShaderGUI"
}
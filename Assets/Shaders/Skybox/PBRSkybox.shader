Shader "Custom/Skybox/PBRSkybox"
{
	Properties
	{
		_SkyboxColor ("Skybox Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Thickness ("Atmosphere Thickness (km)", Range(0.0, 1000.0)) = 160.0
		_Radius ("Planet Radius (km)", Float) = 6400.0
		_RScale ("Radius And Thicknesss Scale", Range(0.0, 10.0)) = 1.0
		[IntRange] _SampleCounts ("Sample Counts", Range(0, 60)) = 30
		_Exposure ("Exposure", Range(0.0, 2.0)) = 1.0
		
		[Space]
		_ColorRed ("Red Wave Color", Color) = (1.0, 0.0, 0.0, 1.0)
		_ColorGreen ("Green Wave Color", Color) = (0.0, 1.0, 0.0, 1.0)
		_ColorBlue ("Blue Wave Color", Color) = (0.0, 0.0, 1.0, 1.0)
		_NightColor ("Night Color", Color) = (0.0, 0.0, 0.0, 1.0)
		_SunColor ("Sun Color", Color) = (1.0, 1.0, 1.0, 1.0)
		
		[Space]
		_gMie ("Mie coefficient g", Range(-0.2, 0.2)) = 0.1
		_mieFac ("Mie Fac", Range(0.0, 1.0)) = 0.2
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
			
			#include "Assets/Shaders/Skybox/PBRSkyboxPass.hlsl"
			
			ENDHLSL
		}
	}
}
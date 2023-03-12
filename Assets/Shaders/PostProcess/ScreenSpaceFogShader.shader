Shader "Hidden/Custom/Post-processing/Screen Space Fog"
{
	Properties
	{
		_LightColor ("Light Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_Thickness ("Atmosphere Thickness (km)", Range(0.0, 1000.0)) = 160.0
		_Radius ("Planet Radius (km)", Float) = 6400.0
		_RScale ("Radius And Thicknesss Scale", Range(0.0, 10.0)) = 1.0
		[IntRange] _SampleCounts ("Sample Counts", Range(0, 60)) = 30
		_Exposure ("Exposure", Range(0.0, 2.0)) = 1.0
		
		[Space]
		_gMie ("Mie Coefficient g", Range(-0.2, 0.2)) = 0.1
		_mieFac ("Mie Fac", Range(0.0, 1.0)) = 0.2
		_rayleighFac ("Rayleigh Fac", Range(0.0, 1.0)) = 0.2
	}
	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			Name "Screen Space Fog Pass"
			
			HLSLPROGRAM

			#pragma target 4.5

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT
			
			#pragma vertex ScreenSpaceFogPassVertex
			#pragma fragment ScreenSpaceFogPassFragment

			#include "Assets/Shaders/ShaderLibrary/Utility.hlsl"
			#include "Assets/Shaders/PostProcess/ScreenSpaceFogPass.hlsl"

			ENDHLSL
		}
	}
}
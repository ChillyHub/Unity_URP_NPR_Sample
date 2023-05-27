 Shader "Hidden/Custom/Post-processing/Screen Space Reflection"
{
	Properties
	{
		
	}
	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			Name "Screen Space Reflection Pass"
			
			HLSLPROGRAM

			#pragma target 4.5

			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _SHADOWS_SOFT
			
			#pragma vertex ScreenSpaceReflectionPassVertex
			#pragma fragment ScreenSpaceReflectionPassFragment
			
			#include "Assets/Shaders/PostProcess/ScreenSpaceReflectionPass.hlsl"

			ENDHLSL
		}
	}
}
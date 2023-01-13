Shader "Hidden/Custom/ScreenSpaceRim"
{
	Properties
	{
		_Rim_Intensity ("Rim Intensity", float) = 1.0
		_Rim_Bias ("Rim Bias", float) = 0.2
	}
	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			Name "Screen Space Rim Pass"
			
			HLSLPROGRAM

			#pragma target 4.5

			#pragma vertex ScreenSpaceRimPassVertex
			#pragma fragment ScreenSpaceRimPassFragment

			#include "ScreenSpaceRimPass.hlsl"

			ENDHLSL
		}
	}
}
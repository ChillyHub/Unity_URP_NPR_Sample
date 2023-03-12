Shader "Hidden/Custom/Post-processing/Gran Turismo Tone Mapping"
{
	Properties
	{
		_P("Maximum brightness", Range(0.0, 5.0)) = 1.0
		_a("Slope", Range(0.0, 5.0)) = 1.0
		_m("Linear section start", Range(0.0, 1.0)) = 0.22
		_l("Linear section length", Range(0.0, 1.0)) = 0.4
		_c("Black tightness", Range(0.0, 3.0)) = 1.33
		_b("Darkness value", Range(0.0, 1.0)) = 0.0
	}
	SubShader
	{
		Cull Off 
		ZWrite Off 
		ZTest Always

		Pass
		{
			Name "Gran Turismo Tone Mapping Pass"
			
			HLSLPROGRAM

			#pragma target 4.5

			#pragma vertex GranTurismoToneMappingPassVertex
			#pragma fragment GranTurismoToneMappingPassFragment

			#include "Assets/Shaders/ShaderLibrary/Utility.hlsl"
			#include "Assets/Shaders/PostProcess/GranTurismoToneMappingPass.hlsl"

			ENDHLSL
		}
	}
}
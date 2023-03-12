Shader "Custom/Skybox/NPRSkyboxLowQuality"
{
	Properties
	{
		[Foldout(Color Setting)]
		[HDR] _ColorDayBright("Day Bright Color", Color) = (0.0, 0.0, 1.0, 1.0)
    	[HDR] _ColorDayMiddle("Day Middle Color", Color) = (0.0, 1.0, 0.0, 1.0)
    	[HDR] _ColorDayDark("Day Dark Color", Color) = (1.0, 0.0, 0.0, 1.0)
    	[HDR] _ColorNight("Night Color", Color) = (0.0, 0.0, 0.0, 1.0)
    	[HDR] _ColorSun("Sun Color", Color) = (1.0, 1.0, 1.0, 1.0)
    	[HDR] _ColorMoon("Moon Color", Color) = (0.5, 0.5, 0.5, 1.0)
    	_DayBrightRange("Day Bright Range", Range(0.0, 1.0)) = 1.0
    	_DayMiddleRange("Day Middle Range", Range(0.0, 1.0)) = 0.5
    	_DayDarkRange("Day Dark Range", Range(0.0, 1.0)) = 0.2
    	_Exposure("Exposure", Range(0.0, 10.0)) = 1.0
		[FoldEnd]
    	
		[Foldout(Advance)]
    	_gMie ("Mie Coefficient g", Range(-0.2, 0.2)) = 0.1
		_mieFac ("Mie Fac", Range(0.0, 1.0)) = 0.2
		_gSun ("Sun's Coefficient g", Range(0.999, 1.0)) = 0.9999
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
			
			#pragma vertex NPRSkyboxLowQualityPassVertex
			#pragma fragment NPRSkyboxLowQualityPassFragment
			
			#include "Assets/Shaders/Skybox/NPRSkyboxLowQualityPass.hlsl"
			
			ENDHLSL
		}
	}
	CustomEditor "FoldoutBaseShaderGUI"
}
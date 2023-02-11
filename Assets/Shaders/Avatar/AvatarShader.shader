Shader "Custom/Avatar"
{
    Properties
    {
        [Foldout(Setting)]
    	[Toggle] _NightToggle("Night", Float) = 0
    	_DayTime("Time", Range(0, 24)) = 12
    	[FoldEnd]
    	
    	[Foldout(Textures)]
    	_DiffuseMap("Diffuse Map", 2D) = "white" {}
        _LightMap("Light Map", 2D) = "white" {}
        _RampMap("Ramp Map", 2D) = "white" {}
    	_MetalMap("Metal Map", 2D) = "white" {}
    	_FaceLightMap("Face Light Map", 2D) = "white" {}
    	[FoldEnd]
    	
    	[Foldout(Diffuse, _DIFFUSE_ON)]
    	_AO_Strength("AO Strength",  Range(0, 1)) = 0.3
        _Transition_Range("Transition Range", Range(0, 10)) = 1
        [Toggle(_TRANSITION_BLUR)]_TransitionBlurToggle("Transition Blur", Float) = 1
    	[FoldEnd]
    	
    	[Foldout(Specular, _SPECULAR_ON)]
        _Specular_Range("Specular Range", Range(0, 16)) = 8
    	_Specular_Threshold("Specular Threshold", Range(0, 1)) = 0.5
    	[FoldEnd]
    	
    	[Foldout(Emission, _EMISSION_ON)]
    	_Emission_Strength("Emission Strength", Range(0, 8)) = 1
    	[FoldEnd]
        
    	[Foldout(GI, _GI_ON)]
        _GI_Strength("GI Strength", Range(0, 1)) = 1
        [FoldEnd]
    	
    	[Foldout(Fresnel Rim, _RIM_ON)]
    	_Rim_Color("Rim Color", Color) = (1.0, 1.0, 1.0)
    	_Rim_Strength("Rim Strength", Range(0, 1)) = 0
    	[PowerSlider(4.0)] _Rim_Scale("Rim Scale", Range(0.01, 1)) = 0.08
    	_Rim_Clamp("Rim Clamp", Range(0, 1)) = 0
        [FoldEnd]
    	
    	[Foldout(Edge Rim, _EDGE_RIM_ON)]
    	_Edge_Rim_Strength("Edge Rim Strength", Range(0, 1)) = 1
    	_Edge_Rim_Threshold("Edge Rim Threshold", Range(0.1, 10)) = 0.1
    	_Edge_Rim_Width("Edge Rim Width", Range(0, 3)) = 1
    	[FoldEnd]
        
    	[Foldout(Outline, _OUTLINE_ON)]
        [Toggle(_NORMAL_FIXED)] _NormalFixedToggle("Is Normals Fixed", Float) = 1
        _OutlineColor("Outline Color", Color) = (0.0, 0.0, 0.0, 1.0)
        _OutlineWidth("Outline Width", Float) = 1
    	[FoldEnd]
    	
    	[Foldout(Shadow)]
    	[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadow Caster Type", Float) = 0
		[Toggle(_RECEIVE_SHADOWS)] _ReceiveShadowsToggle("Receive Shadows", Float) = 0
    	[FoldEnd]
        
    	[Foldout(Blend Mode)]
        // Set blend mode
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
		// Default write into depth buffer
		[Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
        // Alpha test
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		[Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        // Alpha premultiply
		[Toggle(_PREMULTIPLY_ALPHA)] _PreMulAlphaToggle("Alpha premultiply", Float) = 0
    	[FoldEnd][HideInInspector] __1("", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        
        HLSLINCLUDE
        #include "../Utility.hlsl"
        ENDHLSL

        Pass
        {
            Name "Avatar Render Pass"
            Tags { "LightMode"="AvatarObject" }
            
            Cull back
        	Blend [_SrcBlend] [_DstBlend], One OneMinusSrcAlpha
			ZWrite [_ZWrite]
            
            HLSLPROGRAM

            #pragma target 4.5

            #pragma shader_feature _ _IS_BODY _IS_HAIR _IS_FACE
            #pragma shader_feature _ _IS_OPAQUE _IS_TRANSPARENT
            #pragma shader_feature _DIFFUSE_ON
            #pragma shader_feature _TRANSITION_BLUR
            #pragma shader_feature _SPECULAR_ON
            #pragma shader_feature _EMISSION_ON
            #pragma shader_feature _GI_ON
            #pragma shader_feature _RIM_ON
            #pragma shader_feature _EDGE_RIM_ON

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN

            #pragma vertex HairRenderPassVertex
            #pragma fragment HairRenderPassFragment

            #include "AvatarRenderPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Outline Render Pass"
            Tags { "LightMode"="AvatarOutline" }
            
            Cull front
            
            HLSLPROGRAM

            #pragma target 4.5

            #pragma shader_feature _OUTLINE_ON
            #pragma shader_feature _NORMAL_FIXED

            #pragma vertex OutlineRenderPassVertex
            #pragma fragment OutlineRenderPassFragment

            #include "OutlineRenderPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Shadow Caster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            // Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma shader_feature _ _IS_OPAQUE _IS_TRANSPARENT

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // This is used during shadow map generation to differentiate between directional and punctual
            // light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    }
    CustomEditor "AvatarMaterialGUI"
}

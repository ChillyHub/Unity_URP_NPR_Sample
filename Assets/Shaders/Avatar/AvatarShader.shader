Shader "Custom/Avatar"
{
    Properties
    {
	    [Foldout(Textures)]
    	_DiffuseMap("Diffuse Map", 2D) = "white" {}
    	_NormalMap("Normal Map", 2D) = "bump" {}
        _LightMap("Light Map", 2D) = "white" {}
        _RampMap("Ramp Map", 2D) = "white" {}
    	_MetalMap("Metal Map", 2D) = "white" {}
    	_FaceLightMap("Face Light Map", 2D) = "white" {}
    	[FoldEnd]
    	
    	[Foldout(Setting)]
        _DayTime("Time", Range(0, 24)) = 12
    	[IntRange] _RampV1("Ramp Line of Mat1 (0.0~0.2)", Range(1, 5)) = 1
    	[IntRange] _RampV2("Ramp Line of Mat2 (0.2~0.4)", Range(1, 5)) = 2
    	[IntRange] _RampV3("Ramp Line of Mat3 (0.4~0.6)", Range(1, 5)) = 3
    	[IntRange] _RampV4("Ramp Line of Mat4 (0.6~0.8)", Range(1, 5)) = 4
    	[IntRange] _RampV5("Ramp Line of Mat5 (0.8~1.0)", Range(1, 5)) = 5
    	[FoldEnd]
    	
    	[Foldout(Diffuse, _DIFFUSE_ON)]
    	_Diffuse_Intensity("Diffuse Intensity", Range(0, 1)) = 1
        _Transition_Range("Transition Range", Range(0, 1)) = 0
        [FoldEnd]
    	
    	[Foldout(Specular, _SPECULAR_ON)]
    	_Specular_Intensity("Specular Intensity", Range(0, 1)) = 1
        _Specular_Range("Specular Range", Range(1, 16)) = 8
    	[Toggle] _MetalSoftSpecToggle("Metal Specular Is Soft", Float) = 1
        [FoldEnd]
    	
    	[Foldout(Emission, _EMISSION_ON)]
    	_Emission_Intensity("Emission Intensity", Range(0, 8)) = 1
	    _Emission_Color("Emission Color", Color) = (1.0, 1.0, 1.0)
    	[Toggle] _Emission_Color_Only("Only Use Emission Color", Float) = 0.0
    	[FoldEnd]
        
    	[Foldout(GI, _GI_ON)]
        _GI_Intensity("GI Intensity", Range(0, 1)) = 1
        [FoldEnd]
    	
    	[Foldout(Fresnel Rim, _RIM_ON)]
    	_Rim_Intensity("Rim Intensity", Range(0, 1)) = 0
    	_Rim_Color("Rim Color", Color) = (1.0, 1.0, 1.0)
        [PowerSlider(4.0)] _Rim_Scale("Rim Scale", Range(0.01, 1)) = 0.08
    	_Rim_Clamp("Rim Clamp", Range(0, 1)) = 0
        [FoldEnd]
    	
    	[Foldout(Edge Rim, _EDGE_RIM_ON)]
    	_Edge_Rim_Intensity("Edge Rim Intensity", Range(0, 1)) = 1
    	_Edge_Rim_Threshold("Edge Rim Threshold", Range(0.1, 10)) = 0.1
    	_Edge_Rim_Width("Edge Rim Width", Range(0, 3)) = 1
    	[FoldEnd]
        
    	[Foldout(Outline, _OUTLINE_ON)]
        [Toggle(_NORMAL_FIXED)] _NormalFixedToggle("Use Smooth Normals", Float) = 0
    	[Toggle(_USE_VERTEX_COLOR)] _UseVertexColorToggle("Use Vertex Color", Float) = 0
    	[Toggle(_USE_VERTEX_ALPHA)] _UseVertexAlphaToggle("Use Vertex Alpha", Float) = 0
        _OutlineColor("Outline Mul Color", Color) = (0.2, 0.2, 0.2, 1.0)
        _OutlineWidth("Outline Width", Float) = 1
    	[FoldEnd]
    	
    	[Foldout(Shadow)]
    	[KeywordEnum(On, Clip, Dither, Off)] _Shadows("Shadow Caster Type", Float) = 0
		[Toggle(_RECEIVE_LIGHT_SHADOWS)] _ReceiveLightShadowsToggle("Receive Light Shadows", Float) = 0
		[Toggle(_RECEIVE_DEPTH_SHADOWS)] _ReceiveDepthShadowsToggle("Receive Depth Shadows", Float) = 0
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
        #include "Assets/Shaders/ShaderLibrary/Utility.hlsl"
        ENDHLSL

        Pass
        {
            Name "Avatar Render Pass"
            Tags { "LightMode" = "AvatarObject" }
            
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
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma vertex HairRenderPassVertex
            #pragma fragment HairRenderPassFragment

			#include "Assets/Shaders/Avatar/AvatarInput.hlsl"
            #include "Assets/Shaders/Avatar/AvatarRenderPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Outline Render Pass"
            Tags { "LightMode" = "AvatarOutline" }

            Cull front
            
            HLSLPROGRAM

            #pragma target 4.5

            #pragma shader_feature _OUTLINE_ON
            #pragma shader_feature _NORMAL_FIXED
            #pragma shader_feature _USE_VERTEX_COLOR
            #pragma shader_feature _USE_VERTEX_ALPHA

            #pragma vertex OutlineRenderPassVertex
            #pragma fragment OutlineRenderPassFragment

			#include "Assets/Shaders/Avatar/AvatarInput.hlsl"
            #include "Assets/Shaders/Avatar/OutlineRenderPass.hlsl"

            ENDHLSL
        }
        Pass
        {
            Name "Shadow Caster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull back

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

            #include "Assets/Shaders/Avatar/AvatarInput.hlsl"
            #include "Assets/Shaders/Avatar/ShadowCasterPass.hlsl"
            
            ENDHLSL
        }
    	Pass
        {
            Name "DepthOnly"
            Tags {"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull back

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

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Assets/Shaders/Avatar/AvatarInput.hlsl"
            #include "Assets/Shaders/Avatar/DepthOnlyPass.hlsl"
            
            ENDHLSL
        }
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "GBuffer"
            Tags {"LightMode" = "UniversalGBuffer"}

            ZWrite[_ZWrite]
            ZTest LEqual
            Cull back

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            //#pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            //#pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            //#pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _RENDER_PASS_ENABLED

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitGBufferPassVertex
            #pragma fragment LitGBufferPassFragment

            #include "Assets/Shaders/Avatar/AvatarInput.hlsl"
            #include "Assets/Shaders/Avatar/GBufferPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "AvatarMaterialGUI"
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using ProfilingScope = UnityEngine.Rendering.ProfilingScope;

namespace PipelineRenderer
{
    [ExecuteAlways]
    public class SkyboxLUTRenderer : MonoBehaviour
    {
        public ComputeShader computeShader;
        public Material material;

        [Serializable]
        public class LutSetting
        {
            [Serializable]
            public enum Size : int
            {
                _16x16 = 16,
                _32x32 = 32,
                _64x64 = 64,
                _128x128 = 128,
                _256x256 = 256,
                _512x512 = 512,
                _1024x1024 = 1024
            }
            
            public Size size = Size._512x512;
        }
        
        public LutSetting LUTSetting;

        public RenderTexture _skyboxLUT;
        private bool _baked = false;

        private static readonly string KernelName = "PBRSkyboxLUTPre";
        private static readonly string TextureName = "_SkyboxLUT";

        private static readonly int LookupTableId = Shader.PropertyToID(TextureName);
        private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int RScaleId = Shader.PropertyToID("_RScale");
        private static readonly int SampleCountsId = Shader.PropertyToID("_SampleCounts");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int GMieId = Shader.PropertyToID("_gMie");
        private static readonly int MieFacId = Shader.PropertyToID("_mieFac");
        private static readonly int GSunId = Shader.PropertyToID("_gSun");

        public void Bake()
        {
            Debug.Log("Skybox LUT Renderer: Bake LUT");
            
            _baked = false;
        }

        private void Start()
        {
            CreateRenderTexture();
            SetMaterial();
        }

        private void Update()
        {
            if (computeShader == null || material == null || _baked)
            {
                return;
            }
            
            UpdateRenderTexture();
            Render();
            
            material.SetTexture(LookupTableId, _skyboxLUT);
            
            _baked = true;
        }

        private void OnDestroy()
        {
            DestroyRenderTexture();
        }

        private void OnValidate()
        {
            _baked = false;
        }

        void CreateRenderTexture()
        {
            if (_skyboxLUT != null)
            {
                _skyboxLUT.Release();
            }
            
            _skyboxLUT = new RenderTexture((int)LUTSetting.size, (int)LUTSetting.size, 0, 
                RenderTextureFormat.RGFloat, RenderTextureReadWrite.Linear);
            _skyboxLUT.wrapMode = TextureWrapMode.Clamp;
            _skyboxLUT.filterMode = FilterMode.Bilinear;
            _skyboxLUT.enableRandomWrite = true;
            _skyboxLUT.useMipMap = false;
            _skyboxLUT.Create();
        }

        void UpdateRenderTexture()
        {
            if (_skyboxLUT == null || _skyboxLUT.width != (int)LUTSetting.size)
            {
                CreateRenderTexture();
                _baked = false;
            }
        }

        void DestroyRenderTexture()
        {
            if (_skyboxLUT)
            {
                _skyboxLUT.Release();
            }
        }

        void SetMaterial()
        {
            
        }

        void Render()
        {
            float thickness = material.GetFloat(ThicknessId);
            float radius = material.GetFloat(RadiusId);
            float rScale = material.GetFloat(RScaleId);
            float sampleCounts = material.GetFloat(SampleCountsId);
            float exposure = material.GetFloat(ExposureId);
            float gMie = material.GetFloat(GMieId);
            float mieFac = material.GetFloat(MieFacId);
            float gSun = material.GetFloat(GSunId);

            int kernel = computeShader.FindKernel(KernelName);
            computeShader.SetTexture(kernel, TextureName, _skyboxLUT);
            computeShader.SetVector(LightColorId, Vector4.one);
            computeShader.SetFloat(ThicknessId, thickness);
            computeShader.SetFloat(RadiusId, radius);
            computeShader.SetFloat(RScaleId, rScale);
            computeShader.SetFloat(SampleCountsId, sampleCounts);
            computeShader.SetFloat(ExposureId, exposure);
            computeShader.SetFloat(GMieId, gMie);
            computeShader.SetFloat(MieFacId, mieFac);
            computeShader.SetFloat(GSunId, gSun);

            uint x, y, z;
            computeShader.GetKernelThreadGroupSizes(kernel, out x, out y, out z);
            computeShader.Dispatch(kernel, (int)LUTSetting.size / (int)x, (int)LUTSetting.size / (int)y, 1);
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class ScreenSpaceFogPass : ScriptableRenderPass
    {
        private const string ScreenSpaceFogShaderName = "Hidden/Custom/Post-processing/Screen Space Fog";

        private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
        private static readonly int ExposureId = Shader.PropertyToID("_Exposure");
        private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");
        private static readonly int RScaleId = Shader.PropertyToID("_RScale");
        private static readonly int RayleighFacId = Shader.PropertyToID("_rayleighFac");
        private static readonly int MieFacId = Shader.PropertyToID("_mieFac");
        private static readonly int GMieId = Shader.PropertyToID("_gMie");
        private static readonly int SampleCountsId = Shader.PropertyToID("_SampleCounts");

        private string _profilerTag;
        private ProfilingSampler _profilingSampler;
        
        private RenderTargetHandle _backRT;
        private Material _material;

        private bool _swap;

        public ScreenSpaceFogPass(string profilerTag)
        {
            Shader shader = Shader.Find(ScreenSpaceFogShaderName);

            if (shader == null)
            {
                Debug.LogError($"Can't find shader {ScreenSpaceFogShaderName}");
            }
            
            this.profilingSampler = new ProfilingSampler(nameof(CustomToneMappingPass));

            _profilerTag = profilerTag;
            _profilingSampler = new ProfilingSampler(profilerTag);
            _material = CoreUtils.CreateEngineMaterial(shader);

            if (_material == null)
            {
                Debug.LogError("Can't create blit material");
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceFog>();
            if (volume == null || !volume.IsActive() || _material == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                RenderTargetHandle source = new RenderTargetHandle();
                RenderTargetHandle dest = new RenderTargetHandle();
                if (_swap)
                {
                    source = _backRT;
                    dest.Init(renderingData.cameraData.renderer.cameraColorTarget);
                }
                else
                {
                    source.Init(renderingData.cameraData.renderer.cameraColorTarget);
                    dest = _backRT;
                }
                
                // Update data
                _material.SetColor(LightColorId, volume.lightColor.value);
                _material.SetFloat(ExposureId, volume.exposure.value);
                _material.SetFloat(ThicknessId, volume.thickness.value);
                _material.SetFloat(RadiusId, volume.radius.value);
                _material.SetFloat(RScaleId, volume.radiusThicknessScale.value);
                _material.SetFloat(RayleighFacId, volume.rayleighFac.value);
                _material.SetFloat(MieFacId, volume.mieFac.value);
                _material.SetFloat(GMieId, volume.mieCoefficientG.value);
                _material.SetFloat(SampleCountsId, volume.sampleCounts.value);

                // Blit
                cmd.SetGlobalTexture("_MainTex", source.Identifier());
                Blit(cmd, source.Identifier(), dest.Identifier(), _material, 0);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Setup(RenderTargetHandle back, RenderPassEvent passEvent, bool swap)
        {
            this.renderPassEvent = passEvent;
            
            _backRT = back;
            _swap = swap;
        }
    }
}
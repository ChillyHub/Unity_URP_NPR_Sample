using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class CustomToneMappingPass : ScriptableRenderPass
    {
        private const string GranTurismoShaderName = "Hidden/Custom/Post-processing/Gran Turismo Tone Mapping";

        private static readonly int PId = Shader.PropertyToID("_P");
        private static readonly int AId = Shader.PropertyToID("_a");
        private static readonly int MId = Shader.PropertyToID("_m");
        private static readonly int LId = Shader.PropertyToID("_l");
        private static readonly int CId = Shader.PropertyToID("_c");
        private static readonly int BId = Shader.PropertyToID("_b");

        private string _profilerTag;
        private ProfilingSampler _profilingSampler;
        
        private RenderTargetHandle _backRT;
        private Material _material;
        private bool _swap;

        public CustomToneMappingPass(string profilerTag)
        {
            Shader shader = Shader.Find(GranTurismoShaderName);

            if (shader == null)
            {
                Debug.LogError($"Can't find shader {GranTurismoShaderName}");
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
            var volume = VolumeManager.instance.stack.GetComponent<CustomToneMapping>();
            if (volume == null || !volume.IsActive() || volume.mode == CustomToneMapping.Mapper.None)
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
                var data = volume.granTurismoData;
                _material.SetFloat(PId, data.maximumBrightness.value);
                _material.SetFloat(AId, data.slope.value);
                _material.SetFloat(MId, data.linearSectionStart.value);
                _material.SetFloat(LId, data.linearSectionLength.value);
                _material.SetFloat(CId, data.blackTightness.value);
                _material.SetFloat(BId, data.darknessValue.value);

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
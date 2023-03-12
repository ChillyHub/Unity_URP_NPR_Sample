using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class FinalBlitPass : ScriptableRenderPass
    {
        private static readonly int PId = Shader.PropertyToID("_P");
        private static readonly int AId = Shader.PropertyToID("_a");
        private static readonly int MId = Shader.PropertyToID("_m");
        private static readonly int LId = Shader.PropertyToID("_l");
        private static readonly int CId = Shader.PropertyToID("_c");
        private static readonly int BId = Shader.PropertyToID("_b");

        private string _profilerTag;
        private ProfilingSampler _profilingSampler;
        
        private RenderTargetHandle _backRT;

        private RenderTexture _tmpRT;
        private bool _releaseTmpRT;

        public FinalBlitPass(string profilerTag, RenderTexture tmpRT, bool releaseTmpRT = true)
        {
            this.profilingSampler = new ProfilingSampler(nameof(CustomToneMappingPass));

            _profilerTag = profilerTag;
            _profilingSampler = new ProfilingSampler(profilerTag);

            _tmpRT = tmpRT;
            _releaseTmpRT = releaseTmpRT;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Blit
                Blit(cmd, _backRT.Identifier(), renderingData.cameraData.renderer.cameraColorTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (_releaseTmpRT)
            {
                RenderTexture.ReleaseTemporary(_tmpRT);
            }
        }
        
        public void Setup(RenderTargetHandle back, RenderPassEvent passEvent)
        {
            this.renderPassEvent = passEvent;
            
            _backRT = back;
        }

        public void Setup(RenderTargetHandle back, RenderPassEvent passEvent, bool releaseTmpRT)
        {
            Setup(back, passEvent);
            _releaseTmpRT = releaseTmpRT;
        }
    }
}
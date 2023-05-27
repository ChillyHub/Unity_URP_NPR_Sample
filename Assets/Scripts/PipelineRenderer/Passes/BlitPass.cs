using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class BlitPass : ScriptableRenderPass
    {
        private const string SourceRenderTargetTag = "_SourceTexture";
        
        private string _profilerTag;
        private ProfilingSampler _profilingSampler;

        private RenderTargetHandle _sourceRT = new RenderTargetHandle();
        private RenderTargetHandle _tempRT = new RenderTargetHandle();
        private RenderTargetHandle _destRT = new RenderTargetHandle();
        private Material _material;

        private RenderTextureFormat _renderTextureFormat;
        private FilterMode _filterMode;

        private int _passId;
        
        public BlitPass(string profilerTag, Shader shader, RenderPassEvent renderPassEvent)
        {
            this.profilingSampler = new ProfilingSampler(nameof(BlitPass));
            this.renderPassEvent = renderPassEvent;

            _profilerTag = profilerTag;
            _profilingSampler = new ProfilingSampler(profilerTag);
            _material = CoreUtils.CreateEngineMaterial(shader);

            if (_material == null)
            {
                Debug.LogError("Can't create blit material");
            }

            _renderTextureFormat = RenderTextureFormat.DefaultHDR;
            _filterMode = FilterMode.Bilinear;

            _passId = 0;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            _sourceRT.Init(renderingData.cameraData.renderer.cameraColorTarget);
            _tempRT.Init(new RenderTargetIdentifier("_TemporaryColorTexture"));
            _destRT.Init(renderingData.cameraData.renderer.cameraColorTarget);
            
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            //descriptor.colorFormat = _renderTextureFormat;
            //descriptor.depthBufferBits = 0;
            //descriptor.msaaSamples = 1;
            
            if (_sourceRT.Identifier() == _destRT.Identifier())
            {
                cmd.GetTemporaryRT(_tempRT.id, descriptor, _filterMode);
            }
            else
            {
                cmd.GetTemporaryRT(_destRT.id, descriptor, _filterMode);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
            {
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Blit
                if (_sourceRT.Identifier() == _destRT.Identifier())
                {
                    cmd.SetGlobalTexture("_SourceTex", _sourceRT.Identifier());
                    Blit(cmd, _sourceRT.Identifier(), _tempRT.Identifier(), _material, _passId);
                    
                    cmd.SetGlobalTexture("_SourceTex", _tempRT.Identifier());
                    Blit(cmd, _tempRT.Identifier(), _destRT.Identifier());
                }
                else
                {
                    cmd.SetGlobalTexture("_SourceTex", _sourceRT.Identifier());
                    Blit(cmd, _sourceRT.Identifier(), _destRT.Identifier(), _material, _passId);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (_sourceRT.Identifier() == _destRT.Identifier())
            {
                cmd.ReleaseTemporaryRT(_tempRT.id);
            }
            else
            {
                cmd.ReleaseTemporaryRT(_destRT.id);
            }
        }
    }
}
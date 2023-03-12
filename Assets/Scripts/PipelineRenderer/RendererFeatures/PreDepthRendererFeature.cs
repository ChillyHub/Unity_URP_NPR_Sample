using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class PreDepthRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent @event = RenderPassEvent.BeforeRenderingGbuffer;
        public LayerMask layerMask;
        public string renderTargetTag = "_DepthTextures";

        private const string PassTag = "PreDepthFeature";
        
        private PreDepthPass _preDepthPass;
        
        public override void Create()
        {
            _preDepthPass = new PreDepthPass(PassTag, @event, (int)layerMask, renderTargetTag);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // _preDepthPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(_preDepthPass);
        }
    }
}
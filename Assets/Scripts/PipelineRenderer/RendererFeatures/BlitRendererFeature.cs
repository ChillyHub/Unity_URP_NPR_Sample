using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class BlitRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent @event = RenderPassEvent.AfterRenderingPostProcessing;
        public Shader shader;

        private const string ProfilerTag = "BlitPass";

        private BlitPass _blitPass;
        
        public override void Create()
        {
            _blitPass = new BlitPass(ProfilerTag, shader, @event);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_blitPass);
        }
    }
}
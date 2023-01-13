using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using CustomRenderer.Passes;

namespace CustomRenderer.RendererFeatures
{
    public class ScreenSpaceRimRendererFeature : ScriptableRendererFeature
    {
        public RenderPassEvent Event = RenderPassEvent.BeforeRenderingPostProcessing;
        
        private ScreenSpaceRimPass _screenSpaceRimPass;
        
        public override void Create()
        {
            _screenSpaceRimPass = new ScreenSpaceRimPass(Event);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            _screenSpaceRimPass.Setup(renderer.cameraColorTarget);
            renderer.EnqueuePass(_screenSpaceRimPass);
        }
    }
}
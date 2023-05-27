using System;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class PostProcessRendererFeature : ScriptableRendererFeature
    {
        private const string ProfilerTag = "Custom Render PostProcessing Effects";
        
        private RenderTargetHandle _backRT;
        private RenderTexture _backBuffer = null;
        private bool _swap = false;

        private ScreenSpaceFogPass _screenSpaceFogPass;
        private ScreenSpaceReflectionPass _screenSpaceReflectionPass;
        private CustomToneMappingPass _toneMappingPass;
        private FinalBlitPass _beforePostFinalBlitPass;
        private FinalBlitPass _afterPostFinalBlitPass;

        public override void Create()
        {
            _screenSpaceFogPass = new ScreenSpaceFogPass(ProfilerTag);
            _screenSpaceReflectionPass = new ScreenSpaceReflectionPass(ProfilerTag);
            _toneMappingPass = new CustomToneMappingPass(ProfilerTag);
            _beforePostFinalBlitPass = new FinalBlitPass(ProfilerTag, _backBuffer, false);
            _afterPostFinalBlitPass = new FinalBlitPass(ProfilerTag, _backBuffer, true);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!renderingData.cameraData.postProcessEnabled)
            {
                return;
            }

            if (_backBuffer != null)
            {
                RenderTexture.ReleaseTemporary(_backBuffer);
            }
            _backBuffer = RenderTexture.GetTemporary(renderingData.cameraData.cameraTargetDescriptor);

            if (!_backBuffer.IsCreated())
            {
                _backBuffer.Create();
            }
            
            _backRT.Init(new RenderTargetIdentifier(_backBuffer));

            // AfterRenderingTransparent
            bool renderFinalPass = false;

            if (VolumeManager.instance.stack.GetComponent<ScreenSpaceFog>().IsActive())
            {
                _screenSpaceFogPass.Setup(_backRT, RenderPassEvent.AfterRenderingTransparents, _swap);
                renderer.EnqueuePass(_screenSpaceFogPass);
                SwapRenderTarget();
                
                renderFinalPass = true;
            }
            
            if (VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>().IsActive())
            {
                _screenSpaceReflectionPass.Setup(_backRT, RenderPassEvent.AfterRenderingTransparents, _swap);
                renderer.EnqueuePass(_screenSpaceReflectionPass);
                SwapRenderTarget();

                renderFinalPass = true;
            }

            if (renderFinalPass && _swap)
            {
                _beforePostFinalBlitPass.Setup(_backRT, RenderPassEvent.AfterRenderingTransparents);
                renderer.EnqueuePass(_beforePostFinalBlitPass);
                SwapRenderTarget();
            }
            
            // AfterRenderingPostProcessing
            renderFinalPass = false;

            if (VolumeManager.instance.stack.GetComponent<CustomToneMapping>().IsActive())
            {
                _toneMappingPass.Setup(_backRT, RenderPassEvent.AfterRendering, _swap);
                renderer.EnqueuePass(_toneMappingPass);
                SwapRenderTarget();

                renderFinalPass = true;
            }

            if (renderFinalPass && _swap)
            {
                _afterPostFinalBlitPass.Setup(_backRT, RenderPassEvent.AfterRendering);
                renderer.EnqueuePass(_afterPostFinalBlitPass);
                SwapRenderTarget();
            }
        }

        void SwapRenderTarget()
        {
            _swap = !_swap;
        }
    }
}
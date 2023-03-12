using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class ScreenSpaceRimPass : ScriptableRenderPass
    {
        private ScreenSpaceRim _screenSpaceRim;
        private Material _material;

        private Vector2Int _bufferSize;
        private RenderTargetIdentifier _renderTarget;

        #region ShaderConstants

        private const string ShaderName = "Hidden/Custom/ScreenSpaceRim";
        private const string RenderPostProcessingTag = "Render Screen Space Rim";

        private static readonly int RimIntensityId = Shader.PropertyToID("_Rim_Intensity");
        private static readonly int RimBiasId = Shader.PropertyToID("_Rim_Bias");
        private static readonly int RimThreshold = Shader.PropertyToID("_Rim_Threshold");
 
        private static readonly ProfilingSampler ProfilingRenderPostProcessing =
            new ProfilingSampler(RenderPostProcessingTag);

        #endregion

        public ScreenSpaceRimPass(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
            
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"Can't find shader {ShaderName}");
            }

            _material = CoreUtils.CreateEngineMaterial(shader);
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
            {
                Debug.LogError("Can't create screen space rim material");
                return;
            }

            if (!renderingData.postProcessingEnabled)
            {
                return;
            }

            _screenSpaceRim = VolumeManager.instance.stack.GetComponent<ScreenSpaceRim>();
            if (_screenSpaceRim == null || !_screenSpaceRim.IsActive())
            {
                return;
            }

            _bufferSize.x = renderingData.cameraData.camera.scaledPixelWidth;
            _bufferSize.y = renderingData.cameraData.camera.scaledPixelHeight;

            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingRenderPostProcessing))
            {
                Render(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Setup(RenderTargetIdentifier renderTarget)
        {
            _renderTarget = renderTarget;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureFormat format = renderingData.cameraData.isHdrEnabled
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.Default;
            
            _material.SetFloat(RimIntensityId, _screenSpaceRim.intensity.value);
            _material.SetFloat(RimBiasId, _screenSpaceRim.bias.value);
            _material.SetFloat(RimThreshold, _screenSpaceRim.threshold.value);

            cmd.SetRenderTarget(_renderTarget, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, _material, 0, MeshTopology.Triangles, 3);
        }
    }
}
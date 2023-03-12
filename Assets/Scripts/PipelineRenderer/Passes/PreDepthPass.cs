using System;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    public class PreDepthPass : ScriptableRenderPass
    {
        private string _profilerTag;
        private FilteringSettings _filteringSettings;
        private ProfilingSampler _profilingSampler;
        private List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();
        private RenderStateBlock _renderStateBlock;

        private RenderTargetHandle _renderTarget;
        
        public PreDepthPass(string profilerTag, RenderPassEvent renderPassEvent, int layerMask, 
            string renderTargetTag)
        {
            RenderQueueRange renderQueueRange = RenderQueueRange.all;

            this.profilingSampler = new ProfilingSampler(nameof(PreDepthPass));
            this.renderPassEvent = renderPassEvent;

            _profilerTag = profilerTag;
            _filteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            _profilingSampler = new ProfilingSampler(profilerTag);
            _shaderTagIdList.Add(new ShaderTagId("DepthOnly"));
            _renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            
            _renderTarget.Init(new RenderTargetIdentifier("_CameraDepthTexture"));
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.colorFormat = RenderTextureFormat.Depth;
            descriptor.depthBufferBits = 32;
            descriptor.msaaSamples = 1;
            cmd.GetTemporaryRT(_renderTarget.id, descriptor, FilterMode.Point);
            ConfigureTarget(_renderTarget.Identifier());
            ConfigureClear(ClearFlag.None, Color.black);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            SortingCriteria sortingCriteria = renderingData.cameraData.defaultOpaqueSortFlags;

            DrawingSettings drawingSettings = 
                CreateDrawingSettings(_shaderTagIdList, ref renderingData, sortingCriteria);

            ref CameraData cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                // Render the objects...
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, 
                    ref _filteringSettings, ref _renderStateBlock);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            cmd.ReleaseTemporaryRT(_renderTarget.id);
            _renderTarget = RenderTargetHandle.CameraTarget;
        }
    }
}
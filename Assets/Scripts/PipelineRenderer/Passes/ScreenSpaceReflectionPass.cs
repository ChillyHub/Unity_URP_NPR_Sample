using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
// using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PipelineRenderer
{
    public class ScreenSpaceReflectionPass : ScriptableRenderPass
    {
        private const string SSRShaderName = "Hidden/Custom/Post-processing/Screen Space Reflection";

        private string _profilerTag;
        private ProfilingSampler _profilingSampler;
        
        private RenderTargetHandle _backRT;
        private Material _material;
        private bool _swap;

        private Camera _camera;

        public ScreenSpaceReflectionPass(string profilerTag)
        {
            Shader shader = Shader.Find(SSRShaderName);

            if (shader == null)
            {
                Debug.LogError($"Can't find shader {SSRShaderName}");
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
            var volume = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflection>();
            if (volume == null || !volume.IsActive())
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, _profilingSampler))
            {
                // Ensure we flush our command-buffer before we render...
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                // Get Render Targets
                GetSourceDestRT(out RenderTargetHandle source, out RenderTargetHandle dest, ref renderingData);

                _camera = renderingData.cameraData.camera;
                
                // Update data
                float near = _camera.nearClipPlane;
                float far = _camera.farClipPlane;
                float invNear = Mathf.Approximately(near, 0.0f) ? 0.0f : 1.0f / near;
                float invFar = Mathf.Approximately(far, 0.0f) ? 0.0f : 1.0f / far;
                float isOrthographic = _camera.orthographic ? 1.0f : 0.0f;
                
                float zc0 = 1.0f - far * invNear;
                float zc1 = far * invNear;

                Vector4 projectBufferParams = new Vector4(zc0, zc1, zc0 * invFar, zc1 * invFar);

                if (SystemInfo.usesReversedZBuffer)
                {
                    projectBufferParams.y += projectBufferParams.x;
                    projectBufferParams.x = -projectBufferParams.x;
                    projectBufferParams.w += projectBufferParams.z;
                    projectBufferParams.z = -projectBufferParams.z;
                }
                
                _material.SetMatrix("_WorldToViewMatrix", _camera.worldToCameraMatrix);
                _material.SetMatrix("_ViewToHClipMatrix", GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false));
                _material.SetVector("_ProjectBufferParams", projectBufferParams);
                
                // Debug
                _material.SetFloat("_StopIndex", volume.stopIndex.value);
                _material.SetFloat("_MaxLoop", volume.maxLoop.value);
                _material.SetFloat("_Thickness", volume.thickness.value);
                _material.SetInt("_MaxStepSize", volume.maxStepSize.value);

                // Blit
                cmd.SetGlobalTexture("_SourceTex", source.Identifier());
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

        void GetSourceDestRT(out RenderTargetHandle source, out RenderTargetHandle dest, 
            ref RenderingData renderingData)
        {
            source = new RenderTargetHandle();
            dest = new RenderTargetHandle();
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
        }
    }
}
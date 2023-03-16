using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PipelineRenderer
{
    [ExecuteAlways]
    public class PlaneReflectRenderer : MonoBehaviour
    {
        public LayerMask layerMask;
        
        private static readonly int RefColorTexId = Shader.PropertyToID("_RefColorTex");

        private GameObject _cameraObject;
        private Camera _reflectionCamera;
        private MeshRenderer _meshRenderer;
        private RenderTexture _renderTexture;

        private bool _isRegisterDelegate = false;

        private void OnEnable()
        {
            Init();
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        private void Update()
        {
            if (_renderTexture != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetTexture(RefColorTexId, _renderTexture);
                _meshRenderer.SetPropertyBlock(block);
            }
        }

        void Init()
        {
            if (_isRegisterDelegate == false)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                _isRegisterDelegate = true;
            }
            
            _meshRenderer = GetComponent<MeshRenderer>();
        }

        void Release()
        {
            if (_isRegisterDelegate)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                _isRegisterDelegate = false;
            }

            if (_reflectionCamera != null)
            {
                _reflectionCamera.targetTexture = null;
            }

            if (_renderTexture != null)
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                _renderTexture.Release();
            }

            if (_cameraObject != null)
            {
                DestroyImmediate(_cameraObject);
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            if (cam.cameraType == CameraType.Reflection || cam.cameraType == CameraType.Preview)
            {
                return;
            }

            UpdateRenderTexture(cam);
            UpdateReflectionCamera(cam);

            Render(context, _reflectionCamera);
        }

        void CreateReflectionCamera()
        {
            _cameraObject = new GameObject(name + " Reflection Camera", 
                typeof(Camera), typeof(UniversalAdditionalCameraData));
            _cameraObject.hideFlags = HideFlags.HideAndDontSave;
            
            var data = _cameraObject.GetComponent<UniversalAdditionalCameraData>();
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresColorOption = CameraOverrideOption.Off;
            data.requiresColorTexture = false;
            data.requiresDepthTexture = false;
            data.renderPostProcessing = false;
            data.renderShadows = false;

            _reflectionCamera = _cameraObject.GetComponent<Camera>();
            _reflectionCamera.enabled = false;
        }

        void CreateRenderTexture(Camera cam)
        {
            int width = GetScaledLength(cam.pixelWidth);
            int height = GetScaledLength(cam.pixelHeight);
            int depthbuffer = 32;
            RenderTextureFormat format = UniversalRenderPipeline.asset.supportsHDR
                ? RenderTextureFormat.DefaultHDR
                : RenderTextureFormat.Default;
            
            _renderTexture = RenderTexture.GetTemporary(width, height, depthbuffer, format);
            _renderTexture.filterMode = FilterMode.Trilinear;
            _renderTexture.useMipMap = true;
        }

        void UpdateReflectionCamera(Camera cam)
        {
            if (_reflectionCamera == null)
            {
                CreateReflectionCamera();
            }
            
            _reflectionCamera.CopyFrom(cam);
            _reflectionCamera.cullingMask = (int)layerMask;
            _reflectionCamera.useOcclusionCulling = false;
            _reflectionCamera.targetTexture = _renderTexture;

            _reflectionCamera.worldToCameraMatrix *= GetReflectionMatrix(transform.position, transform.up);
            _reflectionCamera.projectionMatrix = CalculateObliqueMatrix(cam, transform.position, -transform.up);
        }

        void UpdateRenderTexture(Camera cam)
        {
            if (_renderTexture == null)
            {
                CreateRenderTexture(cam);
            }
            else if (_renderTexture.width != GetScaledLength(cam.pixelWidth) ||
                     _renderTexture.height != GetScaledLength(cam.pixelHeight))
            {
                RenderTexture.ReleaseTemporary(_renderTexture);
                CreateRenderTexture(cam);
            }
        }

        void Render(ScriptableRenderContext context, Camera cam)
        {
            GL.invertCulling = !GL.invertCulling;
            UniversalRenderPipeline.RenderSingleCamera(context, cam);
            GL.invertCulling = !GL.invertCulling;
        }

        Matrix4x4 GetReflectionMatrix(Vector3 planePosition, Vector3 planeNormal)
        {
            Matrix4x4 m = Matrix4x4.identity;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    m[i, j] -= 2.0f * planeNormal[i] * planeNormal[j];
                }
                m[i, 3] += 2.0f * planeNormal[i] * Vector3.Dot(planePosition, planeNormal);
            }

            return m;
        }

        int GetScaledLength(int len)
        {
            return (int)(len * UniversalRenderPipeline.asset.renderScale);
        }

        Vector4 GetViewPlane(Camera cam, Vector3 position, Vector3 normal)
        {
            Vector3 viewPos = cam.worldToCameraMatrix.MultiplyPoint(position);
            Vector3 viewNor = cam.worldToCameraMatrix.MultiplyVector(normal);
            float distance = -Vector3.Dot(viewPos, viewNor);
            return new Vector4(viewNor.x, viewNor.y, viewNor.z, distance);
        }

        Matrix4x4 CalculateObliqueMatrix(Camera cam, Vector3 position, Vector3 normal)
        {
            Matrix4x4 projMatrix = cam.projectionMatrix;
            Vector4 viewPlane = GetViewPlane(cam, position, normal);
            Vector4 clipFarP = new Vector4(Mathf.Sign(viewPlane.x), Mathf.Sign(viewPlane.y), 1.0f, 1.0f);
            Vector4 viewFarP = projMatrix.inverse * clipFarP;
            Vector4 m3 = 2.0f * viewPlane / Vector4.Dot(viewPlane, viewFarP) - projMatrix.GetRow(3);
            projMatrix.SetRow(2, m3);
            return projMatrix;
        }
    }
}

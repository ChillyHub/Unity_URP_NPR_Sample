using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Controller.RenderController
{
    [ExecuteAlways]
    public class PlaneReflectRenderController : MonoBehaviour
    {
        public GameObject reflectionCamera;
        
        private static readonly int RefColorTexId = Shader.PropertyToID("_RefColorTex");

        private Camera _reflectionCamera;
        private RenderTexture _renderTexture;

        private bool _isPassAdded = false;
        
        private void Start()
        {
            Init();
        }

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

        void Init()
        {
            if (!_isPassAdded)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
                _isPassAdded = true;
            }
        }

        void Release()
        {
            if (_isPassAdded)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
                _isPassAdded = false;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera.cameraType == CameraType.Reflection || camera.cameraType == CameraType.Preview)
            {
                return;
            }
            
            if (_reflectionCamera == null)
            {
                CreateReflectionCamera(camera);
            }
            if (_renderTexture == null)
            {
                CreateRenderTexture(camera);
            }

            UpdateReflectionCamera(camera);
            
            Render(context, _reflectionCamera);
            
            Shader.SetGlobalTexture(RefColorTexId, _renderTexture);
        }

        void CreateReflectionCamera(Camera camera)
        {
            //gameObject.AddComponent<Camera>();
            //_reflectionCamera = GetComponent<Camera>();

            if (reflectionCamera == null)
            {
                reflectionCamera = new GameObject(name + " Reflection Camera", typeof(Camera));
            }

            _reflectionCamera = reflectionCamera.GetComponent<Camera>();

            if (_reflectionCamera == null)
            {
                Debug.LogError("Can not find reflection camera");
            }
        }

        void CreateRenderTexture(Camera camera)
        {
            _renderTexture = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 16,
                camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        }

        void UpdateReflectionCamera(Camera camera)
        {
            int cullingMask = _reflectionCamera.cullingMask;
            _reflectionCamera.CopyFrom(camera);
            _reflectionCamera.cullingMask = cullingMask;
            _reflectionCamera.useOcclusionCulling = false;
            _reflectionCamera.targetTexture = _renderTexture;
            
            if (_reflectionCamera.TryGetComponent(out UniversalAdditionalCameraData data))
            {
                data.renderShadows = false;
            }
            _reflectionCamera.worldToCameraMatrix *= GetReflectionMatrix(transform.position, transform.up);
        }

        void Render(ScriptableRenderContext context, Camera camera)
        {
            GL.invertCulling = !GL.invertCulling;
            UniversalRenderPipeline.RenderSingleCamera(context, camera);
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
                m[i, 3] -= 2.0f * planeNormal[i] * Vector3.Dot(planePosition, planeNormal);
            }

            return m;
        }
    }
}

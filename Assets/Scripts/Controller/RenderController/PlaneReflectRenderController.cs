using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Controller.RenderController
{
    [ExecuteAlways]
    public class PlaneReflectRenderController : MonoBehaviour
    {
        private static readonly int ColorTexId = Shader.PropertyToID("_ColorTex");
        
        private RenderTexture _renderTexture;
        
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
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        void Release()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            if (_renderTexture != null)
            {
                _renderTexture.Release();
            }
        }

        void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            Camera reflectionCamera = GetReflectionCamera(camera);

            Render(context, reflectionCamera);

            Shader.SetGlobalTexture(ColorTexId, _renderTexture);
        }

        void CreateRenderTexture(Camera camera)
        {
            _renderTexture = RenderTexture.GetTemporary(camera.pixelWidth, camera.pixelHeight, 16,
                camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
        }

        void Render(ScriptableRenderContext context, Camera camera)
        {
            GL.invertCulling = !GL.invertCulling;
            UniversalRenderPipeline.RenderSingleCamera(context, camera);
            GL.invertCulling = !GL.invertCulling;
        }

        Camera GetReflectionCamera(Camera camera)
        {
            if (_renderTexture == null)
            {
                CreateRenderTexture(camera);
            }
            camera.targetTexture = _renderTexture;

            camera.worldToCameraMatrix *= GetReflectionMatrix(transform.position, transform.up);

            return camera;
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

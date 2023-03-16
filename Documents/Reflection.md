# 镜面反射的实现



[TOC]

<img src="./assets/屏幕截图 2023-03-16 191407.png" alt="屏幕截图 2023-03-16 191407" />

### 几种实现反射的方法

- **使用反射探针。**
  利用反射探针来获得物体大概的反射信息。反射探针可以预计算烘培，也可以实时更新新。

  ##### 优点：

  对于静态物体，与烘培反射可以节省计算。质量与性能取决于烘培贴图大小。

  ##### 缺点：

  对于动态物体，需要实时更新反射探针，是一笔不小的开销。
  实现平面反射需要特例（特别处理）。使用 Box Projection Reflection Probe，修正反射光线的路径。

- **使用多道 Pass 渲染**
  让视角变换相对于反射平面对称，再通过 斜视锥体裁剪，渲染出反射的图像，在于平面材质叠加。

  ##### 优点：

  平面反射质量好。相比反射探针处理动态更优。常用于前向渲染。

  ##### 缺点：

  需要多出不少 DrawCall，反射物体太多影响性能。而且只能处理平面。

  ##### 是否使用模板：

  使用模板来剔除需要反射的像素，可以减少 OverDraw，但是要多出道 pass 专门渲染模板。

- **使用 Screen Space Reflection（SSR），屏幕空间反射**
  常用于延迟渲染，利用 GBuffer 的信息，从屏幕空间获取反射图像。

  ##### 优点：

  利用上延迟渲染已有的 Gbuffer，更低的代价取得不错的反射效果。

  ##### 缺点：

  前向渲染需要额外获取 法线等屏幕空间信息，需要不少额外的开销。
  屏幕外的信息和屏幕中被遮挡的信息无法获取，给反射的效果带来瑕疵。



### 使用多 Pass 实现平面反射

获得视角矩阵关于平面反射后的结果，使用反射矩阵进行转换：

```c#
// 获取反射矩阵
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
```



为了裁剪掉平面以下不应该被反射的物体，使用斜视锥体矩阵：

```C#
Vector4 GetViewPlane(Camera cam, Vector3 position, Vector3 normal)
{
    Vector3 viewPos = cam.worldToCameraMatrix.MultiplyPoint(position);
    Vector3 viewNor = cam.worldToCameraMatrix.MultiplyVector(normal);
    float distance = -Vector3.Dot(viewPos, viewNor);
    return new Vector4(viewNor.x, viewNor.y, viewNor.z, distance);
}

// 获取斜视锥体矩阵
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
```



实现多 Pass 的渲染脚本：

```C#
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
            // 将反射图像传给 meshRenderer
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            block.SetTexture(RefColorTexId, _renderTexture);
            _meshRenderer.SetPropertyBlock(block);
        }
    }

    void Init()
    {
        if (_isRegisterDelegate == false)
        {
            // URP 实现，使用事件委托，注册相机渲染前的函数操作。
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

    // 主逻辑
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

        // 关掉一些不需要的渲染
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

        // 创建临时 RT，使用 Mipmap， 采样高 mip 来实现高粗糙度的反射
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
        // 需要反转三角面的正反面
        GL.invertCulling = !GL.invertCulling;
        UniversalRenderPipeline.RenderSingleCamera(context, cam);
        GL.invertCulling = !GL.invertCulling;
    }

    int GetScaledLength(int len)
    {
        return (int)(len * UniversalRenderPipeline.asset.renderScale);
    }
}
```



实现了渲染逻辑，最后就是实现渲染材质的 Shader。

这里直接照搬 URP 的 Lit Shader，稍作修改 ，实现用粗糙度，金属度控制镜面反射。

```c
// 修改获取反射 GI 光照的函数，不再使用光照探针获取反射
half3 ReflectivePlaneGlossyEnvironmentReflection(half3 reflectVector, float3 positionWS,
    half perceptualRoughness, half occlusion, float2 screenUV)
{
    #if !defined(_ENVIRONMENTREFLECTIONS_OFF)
    half3 irradiance;
    
    // 采样 _RefColorTex（反射的图像 RT），用粗糙度控制 mip 等级
    half mip = PerceptualRoughnessToMipmapLevel(perceptualRoughness);
    half4 encodedIrradiance = half4(SAMPLE_TEXTURE2D_LOD(_RefColorTex, sampler_RefColorTex, screenUV, mip));

    #if defined(UNITY_USE_NATIVE_HDR)
    irradiance = encodedIrradiance.rgb;
    #else
    irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
    #endif // UNITY_USE_NATIVE_HDR
    return irradiance * occlusion;
    #else
    return _GlossyEnvironmentColor.rgb * occlusion;
    #endif // _ENVIRONMENTREFLECTIONS_OFF
}
```



##### 效果

**材质色为淡蓝，金属度最高，完全光滑**

<img src="./assets/屏幕截图 2023-03-16 201325.png" alt="屏幕截图 2023-03-16 201325" />

**粗糙表面**

<img src="./assets/屏幕截图 2023-03-16 201431.png" alt="屏幕截图 2023-03-16 201431" />

**非金属表面**

<img src="./assets/屏幕截图 2023-03-16 201405.png" alt="屏幕截图 2023-03-16 201405" />



### 使用 SSR 实现平面反射

未完工


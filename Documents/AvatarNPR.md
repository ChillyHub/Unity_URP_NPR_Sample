# 仿原神的NPR角色渲染

[TOC]

### 效果

先直接上效果图

<div align=center>
<img src="./assets/屏幕截图 2023-03-12 141049.png" alt="屏幕截图 2023-03-12 141049" style="zoom: 50%;" />
</div>

<div align=center>
<img src="./assets/屏幕截图 2023-03-12 133132.png" alt="屏幕截图 2023-03-12 133132" />
</div>

<div align=center>
<img src="./assets/屏幕截图 2023-03-12 140450.png" alt="屏幕截图 2023-03-12 140450" style="zoom: 80%;" />
</div>



### 模型准备与导入

- 首先，从模型网站获取 pmx 模型（我们用珐姐做例子），之后我们需要将其转换为可导入 Unity 的 fbx 文件。
- 使用 blender，借助 mmd_tools 插件，导入 pmx 模型。
- 由于绘制模型的轮廓最好需要借助顶点色的数据（这点后面讲到 Outline 时再具体说明），我们给顶点色的 rgb 通道涂上表示模型轮廓线的颜色，再在顶点色的 a 通道涂色，用 0~1 的数值来控制描边的粗细。这里主要是将嘴巴周围，眼睛周围，发梢尖端和一些物体相接的地方 a 通道设为0，因为这些地方如果描边，会有些突兀的效果。
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 025603.png" alt="屏幕截图 2023-03-11 025603" style="zoom:50%;" title="顶点色的 rgb 通道表示描边颜色"/></div>
  
  <center>▲ 顶点色的 rgb 通道表示描边颜色</center>
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 025033.png" alt="屏幕截图 2023-03-11 025033" style="zoom: 50%;" />
  </div>
  
  <center>▲ 顶点色的 a 通道表示描边粗细，随便涂了一下</center>
- 处理好模型的顶点后，检查一下模型的顶点数据，骨骼连接等，可以用 blender 的加权法线修改器修正一下法线，最后把不需要的刚体胶囊去掉，就可以导出 fbx 文件了。
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 013805.png" alt="屏幕截图 2023-03-11 013805" />
  </div>

  <center>▲ 加权法线修改器</center>
- 一般来说直接导出就行，不会出什么问题。但因为 blender 是右手坐标系且 z 轴向上，而 Unity 是左手坐标系且 y 轴向上，所以这其中是有坑的。
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 033534.png" alt="屏幕截图 2023-03-11 033534" />
  </div>

  <center>▲ blender 导出 fbx 设置</center>

  如图，导出时默认的坐标转换是适配 Unity 的，不过如果不勾选应用转换，模型不会在导出时改变顶点数据，而是在导入 Unity 时再通过 Transform 给网格应用变换。这可能会引发一些问题，比如顶点传入顶点着色器的法线向量可能**不是归一化**的，容易带来难以预期的错误。所以，还是勾选应用变换。

- 导入  Unity，如果应用了变换，需要勾选 Bake Axis Conversion
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 012949-1678477045115-12.png" alt="屏幕截图 2023-03-11 012949" />
  </div>

  <center>▲ fbx 模型导入设置</center>

### 贴图准备与分析

- 获取原神角色的贴图。分析这些贴图可能的作用，有些贴图通道的作用也是我猜测的，所以可能会造成最终的效果有出入。

- 身体贴图。主要是以下的五类：**Diffuse, Lightmap, Normalmap, Ramp, Metalmap**
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011203.png" alt="屏幕截图 2023-03-11 011203" style="zoom:67%;" />
  </div>
  
  <center>▲ Diffuse 贴图，这张的 a 通道表示自发光强度</center>

  Diffuse 贴图的 rgb 通道自然就是 diffuse color 了，主要是这个贴图 a 通道的作用。其实这是视情况而定的。一般情况，a 通道表示材质自发光的强度，需要注意把这个变量拆出来，但是如果贴图中有对应的网格是透明材质，那 a 通道就是一般意义上表示透明度的。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011246.png" alt="屏幕截图 2023-03-11 011246" style="zoom:67%;" />
  </div>
  
  <center>▲ Ramp 贴图</center>
  
  Ramp 贴图是控制阴影颜色的。可以使用版兰伯特光照模型，值为 0.5~1.0 的地方使用原 diffuse color，而值为 0.0~0.5 的地方是阴影区，可将值映射到 u 坐标的 0到1，从而得到对应的阴影颜色。v 坐标是通过材质的类型区分的，这点下一段在 Lightmap 中讲。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011218.png" alt="屏幕截图 2023-03-11 011218" style="zoom:67%;" />
  </div>
  
  <center>▲ Lightmap 贴图</center>
  
  Lightmap 贴图很关键，但作用也是比较迷惑的。据观察，r 通道应该是表示高光强度的变量，不过当 r = 1.0 是，材质还多了层意义，即该材质为金属，因为金属的漫反射与高光材质处理逻辑与普通材质有所不同，需要单独从中取出表示是否为金属的变量。g 通道表示环境光遮蔽（AO），后面会讲到如何处理 g 的数据。b 通道是我最迷惑的，我认为应该是表示高光范围，b 值越大，其对应材质的 blin-phong 高光范围看起来也会大一些。由于原神中高光边缘是硬的，我认为可以将这个值用作高 step 高光的阈值 threshold （仅仅是我的猜测与推断）。a 值用来区分材质类型，在 Ramp 贴图的采样中用来计算 uv 的 v 坐标。不过一般来说，将取出 a 值经过区分白天和黑夜的坐标重映射后，直接采样应该更简洁高效。但似乎不同材质应该采样的 ramp 图 v 坐标与 a 值的大小不是一一对应的（不是连续的），没办法，只能分段控制具体材质对应的 v 坐标le。这里是用来映射 v 坐标的函数。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011231.png" alt="屏幕截图 2023-03-11 011231" style="zoom:67%;" />
  </div>
  
  <center>▲ Normalmap 贴图</center>
  
  Normal map 就是法线贴图，用法这里就不需要多讲了，了解 TBN 矩阵的变换即可。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011422.png" alt="屏幕截图 2023-03-11 011422" style="zoom:67%;" />
  </div>

  <center>▲ Metalmap 贴图</center>
  
  Metal map 是用来表现金属高光的。需要将 normal 从世界空间（World Space）转换到相机空间（View Space），从而映射到uv坐标，其采样值可与 diffuse color 正片叠底，获得视角高光。
  
  
  
- 脸部贴图。主要是 **Diffuse，Shadow，FaceLightmap， Ramp** 四类
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011303.png" alt="屏幕截图 2023-03-11 011303" style="zoom:67%;" />
  </div>
  
  <center>▲ Diffuse 贴图</center>

  脸部的贴图有些许不同，Diffuse 贴图的 a 通道控制脸部腮红的区域。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011400.png" alt="屏幕截图 2023-03-11 011400" style="zoom:67%;" />
  </div>
  
  <center>▲ Diffuse 贴图</center>
  
  Shadow 贴图则有些不明所以。据我的推测，r 通道用来划分材质，b 通道表示 AO（可以保证下巴与脖子之间颜色过渡的衔接），ga 通道没用用处。
  
  
  
  脸部没用专门的 Ramp 图，至于用 Body 的还是 Hair 的视情况而定。
  
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 011411.png" alt="屏幕截图 2023-03-11 011411" style="zoom:67%;" />
  </div>
  
  <center>▲ FaceLightmap 贴图</center>
  
  最重要的还是 Facelightmap 这张 sdf 贴图，控制脸部阴影的，它的值表示是否是阴影的阈值，需要根据光照方向使用不同的 UV。后面再讲具体用法。
  
  
  
- 头发贴图
  思路与身体贴图基本一致，这里不再赘述。



### 贴图导入设置

- Diffuse 贴图默认设置就好。
- Lightmap 贴图导入设置不要勾选 sRGB。 Facelightmap，Metal map 同样。
- Normal map 贴图自然是选择 normal map 模式。
- Ramp 贴图不要勾选生成 mipmap，另外 warp 模式改成 Clamp，防止边界错误采样。
- 还有，所有贴图导入压缩使用高质量，不然效果会打折扣。

<div align=center>
<img src="./assets/屏幕截图 2023-03-11 040550.png" alt="屏幕截图 2023-03-11 040550" style="zoom:67%;" />
</div>

<center>▲ Ramp 贴图导入设置例子</center>

### 确定渲染管线

- 由于做屏幕空间的边缘光需要提前获取深度图，使用**延迟渲染**是不错的选择（前向也可以，带宽压力大只能用前向），不过由于 NPR 渲染的特殊性，**人物的渲染还是走前向渲染的逻辑**。于是，我们可以借住 URP 的自带的 RendererFeature ：Render Objects。我们把角色置于叫做 Avatar 的渲染层，然后创建 Render Opaque，Render Outline， Render Transparent 三个 Render Objects Feature, 分别在 AfterRenderingOpaque，AfterRenderingSkybox, AfterRenderingTransparent 时执行。Render Outline 在渲染完天空盒再执行是为了防止 OUtline 写入深度图，影响深度边缘光的检测。

- 好的卡通渲染表现离不开后处理，可是 Unity 自带的 Tone Mapping 不太能满足要求，于是需要重写一些后处理 pass。一个办法是直接修改 URP 代码中的 PostProcessPass 文件，添加 Volume 代码，这种办法可以避免多余的性能浪费，但是我为了尽量不改变原始代码，选择写一份 Post Process Renderer Feature。主要思路就是分别管理 AfterRenderingTransparents 的 pass 和 AfterRendering 的 pass，并使用自建的 RenderTarget 作为 backBuffer 与此时相机的 render target 组成交换链，每完成一道 pass，就 swap 一次前后缓冲区（RT），以此尽量减少 Blit 次数，提升性能表现。本节，将使用 Unity 自带的 Bloom 和自定义的基于 Gran Turismo 的 Tone Mapping 组成后处理 Volume；

  <div align=center>
  <img src="./assets/屏幕截图 2023-03-11 222021.png" alt="屏幕截图 2023-03-11 222021" style="zoom:67%;" />
  </div>

<center>▲ RendererFerture 设置</center>

- 由此确定 avatar shader 需要多个 pass，除了渲染物体本身的 pass，渲染轮廓的 pass，还需要 ShadowCaster pass 用来采样阴影（同时也是前向渲染获取人物深度的途径），DepthOnly pass，以及 Gbuffer pass （延迟管线需要，该pass 片段着色器什么都不用做，只要 Gbuffer 获取到深度就可以了）。一切准备好后记得在 Universal Renderer Pipeline Asset 勾选使用 Depth Texture。

### Shader 编写

首先，先在顶点着色器计算好所需的各空间点位置信息，法线信息和 UV 信息。可以用自带的函数 GetVertexPositionInputs 和 GetVertexNormalInputs。由于 Fragment 需要的数据较多，我用一个 Surface 结构存储常用的数据，再接下来用一个 GetSurface 函数来统一计算。

```
struct Surface
{
    // Geometry
    float4 positionCS;
    float3 positionWS;
    float3 positionVS;
    float4 positionNDC;
    float2 baseUV;
    half3 normalWS;
    half3 lightDirWS;
    half3 viewDirWS;
    half3 halfDirWS;
    half NdotL;
    half NdotV;
    half NdotH;
    half LdotV;

    // Material
    float3 diffuseColor;
    half alpha;
    half material;
    half ambientOcclusion;
    half diffuseFac;
    half specularFac;
    half emissionFac;
    half specularThreshold;
    half isMetal;
};
```



1. ##### 漫反射 Diffuse

   身体的 diffuse 的主要思路就是使用半兰伯特光照模型控制阴影颜色，其结果 diffuseFac 已经在 GetSurface 里计算过了。

   ```c
   o.diffuseFac = o.NdotL * 0.5 + 0.5;
   ```

   然后，就涉及 ramp 贴图的采样，由于有日夜两种模式，为了这两种模式间平滑过渡，我选择两种模式都采样，根据时间进行 lerp。另外 AO 通道的信息也要利用。

   ```c
   float3 GetDiffuse(TEXTURE2D(rampMap), SAMPLER(samplerRampMap), Surface surface, Light light, float dayTime,
       float transitionRange, float isReceiveLightShadows = false, float isReceiveDepthShadows = false)
   {
   #if defined(_DIFFUSE_ON)
   	// 是否处于阴影下，一般，不接受自阴影
       half shadow = lerp(1.0, light.shadowAttenuation, isReceiveLightShadows);
       // 根据光照获取 u 坐标
       half offsetU = GetRampU(surface, shadow, transitionRange);
       // 根据材质类型获取 v 坐标
       half2 offsetV = GetRampVDayNight(surface.material);
       half offsetVDay = offsetV.x;
       half offsetVNight = offsetV.y;
       // 日夜插值，用于 lerp
       half dayOrNight = smoothstep(4.0, 8.0, abs(dayTime - 12.0));
       // 得到最暗的阴影颜色，用于和非阴影颜色混合
       half3 rampColorDarkDay = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(0.0, offsetVDay)).rgb;
       half3 rampColorDarkNight = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(0.0, offsetVNight)).rgb;
       half3 rampColorDay = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(offsetU, offsetVDay)).rgb;
       half3 rampColorNight = SAMPLE_TEXTURE2D(rampMap, samplerRampMap, float2(offsetU, offsetVNight)).rgb;
       // 得到最终的 ramp color
       half3 rampColor = MixRampColor(surface, light,
           rampColorDay, rampColorNight, rampColorDarkDay, rampColorDarkNight, dayOrNight, offsetU);
       
       float3 diffuse = surface.diffuseColor * rampColor;
   #else
       float3 diffuse = float3(0.0, 0.0, 0.0);
   #endif
   
       return diffuse;
   }
   
   half GetRampU(Surface surface, half shadow, half range)
   {
       // 将 AO 信息与 Half Lambert 信息混合，在映射到[0, 2]，因为贴图只有阴影 ramp 信息
       // AO * 1.5 是为了让其最大值大于 1
       half rampU = saturate(min(min(surface.ambientOcclusion * 1.5, surface.diffuseFac), shadow) * 2.0);
       // range 控制阴影过渡的范围
       rampU = range + (1.0 - range) * rampU;
   
       return rampU;
   }
   ```

   不过身上的金属材质由于其特殊性，要单独处理，把其 diffuse 压低，使金属的 diffuse 看起来较暗。我们把混合 ramp 的操作放在 MixRampColor 中

   ```c
   half3 MixRampColor(Surface surface, Light light, half3 rampColorDay, half3 rampColorNight,
       half3 rampColorDarkDay, half3 rampColorDarkNight, half dayOrNight, half offsetU)
   {
       half3 rampColorDark = lerp(rampColorDarkDay, rampColorDarkNight, dayOrNight);
       
       half3 rampColor = lerp(rampColorDay, rampColorNight, dayOrNight);
       // 非阴影部分，ramp color 为白色
       rampColor = lerp(rampColor, float3(1.0, 1.0, 1.0), step(0.99, offsetU));
       
       // 根据光照强度控制非阴影与阴影颜色的混合
       rampColor = lerp(rampColorDark, rampColor, saturate(light.color * light.distanceAttenuation * 2.0 - 1.0));
       // 如果是金属材质，压低 ramp color 值
       rampColor = lerp(rampColor, half3(0.2, 0.2, 0.2), surface.isMetal);
   
       return rampColor;
   }
   ```

   接下来是脸部的渲染，脸部的 diffuse 就需要 sdf 贴图了。首先是 uv 采样；

   ```c
   // In GetSurface()
   
   // 将光照向量根据头部前向量和有向量转换到局部空间，取消 y 轴，确定光线在局部空间 XZ 平面的方位
   half3 lightDirOSXZ = normalize(-half3(dot(rightWS, o.lightDirWS), 0.0, dot(frontWS, o.lightDirWS)));
   // 根据光是在脸的左侧还是右侧，决定是否翻转 UV
   half2 lightMapUV = lerp(half2(1.0 - input.baseUV.x, input.baseUV.y), input.baseUV, step(0, lightDirOSXZ.x));
   
   // Half lambert 光照
   half lightFac = dot(lightDirOSXZ, float3(0.0, 0.0, 1.0)) * 0.5 + 0.5;
   half faceShadowFac = SAMPLE_TEXTURE2D(faceLightmap, samplerFaceLightmpa, lightMapUV).r;
   
   // 如果 lambert 系数小于阴影阈值，则不在阴影内
   o.diffuseFac = smoothstep(lightFac, lightFac + 0.001, faceShadowFac);
   ```

   然后的渲染逻辑与 Body 基本一致，只不过只用采样最暗和最亮的 ramp，所以可以针对 Face Shader 使用不同的 GetRampU() 和 MixRampColor()。

   ```c
   half GetRampU(Surface surface, half shadow, half range)
   {
       // 取最亮
       return 1.0;
   }
   
   half3 MixRampColor(Surface surface, Light light, half3 rampColorDay, half3 rampColorNight,
       half3 rampColorDarkDay, half3 rampColorDarkNight, half dayOrNight, half offsetU)
   {
       // 阴影色
       half3 rampColorDark = lerp(rampColorDarkDay, rampColorDarkNight, dayOrNight);
       
       // 非阴影色
       half3 rampColor = lerp(rampColorDark, float3(1.0, 1.0, 1.0), surface.diffuseFac);
       
       // 混合阴影
       half fac = lerp(saturate(light.color * light.distanceAttenuation * 2.0 - 1.0),
           light.shadowAttenuation, _ReceiveLightShadowsToggle);
       rampColor = lerp(rampColorDark, rampColor, fac);
   
       return rampColor;
   }
   ```

   还有，脸部漫射获取贴图 diffuseColor 时要与 blush 腮红强度混合

   ```c
   o.diffuseColor = lerp(diffuseMap.rgb, blushColor, blushIntensity * o.emissionFac);
   ```
   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 141655.png" alt="屏幕截图 2023-03-12 141655" style="zoom: 50%;" /></div>

   <center>▲ 漫反射的最终效果图</center>

   

2. ##### 高光 Specular

   高光分为视角高光和布林冯高光，视角高光是金属材质才有的。高光部分不好还原，以下是高光的代码 ：

   ```c
   float3 GetSpecular(TEXTURE2D(metalMap), SAMPLER(samplerMetalMap), Surface surface, Light light, float specularRange,
       float isMetalSoftSpec)
   {
   #if defined(_SPECULAR_ON)
       // 视角空间法向
       float3 normalVS = TransformWorldToViewDir(surface.normalWS, true);
       // 从而映射到 metal map 的 uv，这里稍微扩大了一点 x 轴方向采样
       float2 metalUV = float2(normalVS.x * 0.6 + 0.5, normalVS.y * 0.5 + 0.5);
       half metalFac = SAMPLE_TEXTURE2D(metalMap, samplerMetalMap, metalUV).r;
       // 布林冯
       half blinFac = pow(saturate(surface.NdotH), specularRange);
       
       // 有的角色（如宵宫）的金属高光是软的，而有的是硬的，这里用材质属性做区分
       half isSoftSpec = isMetalSoftSpec * surface.isMetal;
       // 获得 clamp 的高光，这里的高光范围是由 lightmap b 通道控制的
       half3 blinSpec = smoothstep(surface.specularThreshold,
           lerp(surface.specularThreshold + 0.001, surface.specularThreshold + 0.4, isSoftSpec), blinFac);
       // 削减金属阴影部分的视角高光
       half3 metalSpec = surface.diffuseColor * lerp(0.2, 1.0, step(0.5, surface.diffuseFac)) * metalFac * surface.isMetal;
   
       // 如果是金属，高光颜色为白，而不是材质自身颜色
       half3 blinSpecColor = lerp(surface.diffuseColor, half3(0.7, 0.7, 0.7), surface.isMetal);
       // 视角 + 布林冯
       float3 specular = light.color * light.distanceAttenuation *
           (blinSpec * blinSpecColor + metalSpec) * surface.specularFac;
   #else
       float3 specular = float3(0.0, 0.0, 0.0);
   #endif
   
       return specular;
   }
   ```

   其中，surface.specularThreshold 是由 lightmap 的 b 通道得来的，其用法只是我的猜测。

   ```c
   o.specularFac = lightMap.r;
   o.specularThreshold = 1.0 - lightMap.b;
   o.isMetal = step(0.95, o.specularFac);
   ```

   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 141753.png" alt="屏幕截图 2023-03-12 141753" style="zoom:50%;" />
   </div>

   <center>▲ 高光反射的最终效果图</center>

   

3. ##### 全局光 GI

   作为动态物体，这里我只考虑接受 light probe 计算的全局光。不过，一般最终结果不加入 GI。

   ```c
   float3 GetGI(Surface surface)
   {
   #if defined(_GI_ON)
       float3 ambient = surface.diffuseColor * SampleLightProbe(surface.positionWS, surface.normalWS);
   #else
       float3 ambient = float3(0.0, 0.0, 0.0);
   #endif
   
       return ambient;
   }
   
   // Sample light probe
   float3 SampleLightProbe(float3 position, float3 normal)
   {
       float4 coefficients[7];
       coefficients[0] = unity_SHAr;
       coefficients[1] = unity_SHAg;
       coefficients[2] = unity_SHAb;
       coefficients[3] = unity_SHBr;
       coefficients[4] = unity_SHBg;
       coefficients[5] = unity_SHBb;
       coefficients[6] = unity_SHC;
       return max(0.0, SampleSH9(coefficients, normal));
   }
   ```

   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 141917.png" alt="屏幕截图 2023-03-12 141917" style="zoom:50%;" />
   </div>

   <center>▲ 全局光照效果，未加入最终的合成效果</center>

   

4. ##### 自发光 Emission

   直接用 diffuse color 乘以自发光强度就好了。

   ```c
   float3 GetEmission(Surface surface, float3 emissionColor, float colorOnly)
   {
   #if defined(_EMISSION_ON)
       // colorOnly 表示是否混合自身颜色
       float3 emission = lerp(surface.diffuseColor * emissionColor, emissionColor, colorOnly) * surface.emissionFac;
   #else
       float3 emission = float3(0.0, 0.0, 0.0);
   #endif
   
       return emission;
   }
   ```

   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 141828.png" alt="屏幕截图 2023-03-12 141828" style="zoom:50%;" />
   </div>

   <center>▲ 自发光的最终效果图</center>

   

5. ##### 边缘光 Rim

   边缘光可分为菲涅尔边缘光和屏幕空间边缘光。其中菲涅尔边缘光不适合用来直接表达卡通渲染的边缘光，一般可用用作特殊效果，如人物进退场，做闪避动作时的视觉表现。代码如下：

   ```c
   float3 GetFresnelRim(Surface surface, float3 rimColor, half rimScale, half rimClamp)
   {
   #if defined(_RIM_ON)
       float3 fresnelRim = rimColor * max(rimClamp, pow(1.0 - max(min(surface.NdotV, 1.0), 0.0), 0.5 / rimScale));
   #else
       float3 fresnelRim = float3(0.0, 0.0, 0.0);
   #endif
   
       return fresnelRim;
   }
   ```

   效果如下：
   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 142026.png" alt="屏幕截图 2023-03-12 142026" style="zoom:50%;" />
   </div>

   <center>▲ 菲涅尔反射效果，不加入最终的合成效果</center>

   

   屏幕空间边缘光就是重点了。思路时获取深度图，偏移一定距离后与当前深度比较，差别大的说明这个像素处于边缘位置，将当前像素作为边缘光处理。

   ```c
   float3 GetEdgeRim(Surface surface, float3 diffuse, float threshold, float width)
   {
   #if !defined(_DIFFUSE_ON)
       diffuse = float3(1.0, 1.0, 1.0);
   #endif
       
   #if defined(_EDGE_RIM_ON)
   	// 在视角空间沿着法线方向偏移
       float3 biasVS = TransformWorldToViewDir(surface.normalWS) * width * 0.003;
       // 获取偏移点的位置
       float3 biasPosVS = surface.positionVS + biasVS;
       float4 biasPosCS = TransformWViewToHClip(biasPosVS);
       // 根据 HClip 空间坐标获取 NDC 坐标
       half2 trueUV = GetNormalizedScreenSpaceUV(surface.positionCS);
       // 注意在 Fragment 中计算的 HClip 坐标要做透视除法（除以 PosCS.w）
       half2 biasUV = ComputeNormalizedDeviceCoordinates(biasPosCS.xyz / biasPosCS.w);
   
       float depthTrue = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, trueUV);
       float depthBias = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, biasUV);
       // 获得视角空间深度
       float linearDepthTrue = LinearEyeDepth(depthTrue, _ZBufferParams);
       float linearDepthBias = LinearEyeDepth(depthBias, _ZBufferParams);
   	
   	// 根据深度差和阈值确定是否是边缘
       float isEdge = step(threshold, linearDepthBias - linearDepthTrue);
       // 根据光照与视角的夹角决定边缘光强度
       float strength = min(linearDepthBias - linearDepthTrue, 1.0) * (surface.LdotV * -0.5 + 0.5);
       float3 edgeRim = strength * diffuse * isEdge;
   #else
       float3 edgeRim = float3(0.0, 0.0, 0.0);
   #endif
   
       return edgeRim;
   }
   ```

   另外，脸部硬边缘光有一些小小的不同，为了避免正面看脸时出现边缘光（有点突兀），可以加个判断。

   ```c
   // 根据脸的前向和视角判断边缘光强度
   half faceIntensity = saturate(1.0 - surface.FdotV * 1.5);
   
   float strength = min(linearDepthBias - linearDepthTrue, 1.0) * (surface.LdotV * -0.5 + 0.5) * faceIntensity;
   ```
   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 142115.png" alt="屏幕截图 2023-03-12 142115" style="zoom:50%;" />
   </div>

   <center>▲ 屏幕深度边缘光的最终效果图</center>

   

6. ##### 描边 Outline

   我使用的时法线外扩的描边法。描边有两个问题，一是需要平滑法线，不然有断边，会很丑。第二个问题是在世界空间做法线外扩还是在屏幕空间做法线外扩，两者都可以，这里我更喜欢世界空间的法线外扩。

   关于第一个平滑法线的问题，我们如何计算平滑法线，又该如何传入着色器？可以想到找到相同顶点，求平均法线的方法求平滑法线，不过应该把结果存在哪里？因为角色是蒙皮网格，随着物体运动，位置，法线，和切线需要实时更新，不过切线被法线贴图占用了，那如何保证平滑法线的更新呢？
   受法线贴图使用的启发，我们可以通过 TBN 矩阵反求平滑法线在切线空间下的坐标，再将三维的切线空间坐标，压缩成二维，存入 uv3 中。这样，虽然切线空间坐标不变，但随着法线的改变，计算的平滑坐标随之改变。

   可以挂载脚本计算法线，关键函数代码如下：

   ```c#
   public void SmoothNormals()
   {
       var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
       if (skinnedMesh != null)
       {
           mesh = skinnedMesh.sharedMesh;
   
           if (!rebuild)
           {
               return;
           }
   
           Debug.Log("Smoothing normals");
   
           var packNormals = new Vector2[mesh.vertices.Length];
           var smoothNormals = new Vector4[mesh.vertices.Length];
           var verticesGroup = mesh.vertices
               .Select((vertex, index) => (vertex, index)).GroupBy(tuple => tuple.vertex);
   
           // Calculate smooth normals
           foreach (var group in verticesGroup)
           {
               Vector3 smoothNormal = Vector3.zero;
               foreach (var (vertex, index) in group)
               {
                   smoothNormal += mesh.normals[index];
               }
   
               smoothNormal.Normalize();
               foreach (var (vertex, index) in group)
               {
                   smoothNormals[index] = (Vector4)smoothNormal;
               }
           }
   
           // Turn smooth normals from Object space to Tangent space
           for (int index = 0; index < mesh.vertices.Length; index++)
           {
               Vector3 normalOS = mesh.normals[index];
               Vector4 tangentOS = mesh.tangents[index];
               Vector4 bitangentOS = GetBitangentOS(normalOS, tangentOS, skinnedMesh.transform);
               tangentOS.w = 0.0f;
   
               Matrix4x4 tbn = Matrix4x4.identity;
               tbn.SetRow(0, tangentOS.normalized);
               tbn.SetRow(1, bitangentOS);
               tbn.SetRow(2, (Vector4)normalOS.normalized);
   
               Vector4 smoothNormalTS = tbn * smoothNormals[index];
               packNormals[index] = PackNormalOctQuadEncode(smoothNormalTS.normalized);
           }
   
           mesh.uv4 = packNormals;
           rebuild = false;
   
           Debug.Log("Smooth normals completed");
       }
   }
   
   private float GetOddNegativeScale(Transform trans)
   {
       float scale = Vector3.Dot(trans.localScale, Vector3.one);
       return scale >= 0.0f ? 1.0f : -1.0f;
   }
   
   private Vector4 GetBitangentOS(Vector3 normalOS, Vector4 tangentOS, Transform trans)
   {
       Vector3 bitangnet = Vector3.Cross(normalOS.normalized, ((Vector3)tangentOS).normalized) 
                           * (tangentOS.w * GetOddNegativeScale(trans));
   
       bitangnet.Normalize();
       return new Vector4(bitangnet.x, bitangnet.y, bitangnet.z, 0.0f);
   }
   
   private Vector2 PackNormalOctQuadEncode(Vector4 n)
   {
       return PackNormalOctQuadEncode((Vector3)n);
   }
   
   private Vector2 PackNormalOctQuadEncode(Vector3 n)
   {
       float nDot1 = Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
       n /= Mathf.Max(nDot1, 1e-6f);
       float tx = Mathf.Clamp01(-n.z);
       Vector2 t = new Vector2(tx, tx);
       Vector2 res = new Vector2(n.x, n.y);
       return res + (res is { x: >= 0.0f, y: >= 0.0f } ? t : -t);
   }
   ```

   然后就是 OutlinePass 的 顶点着色器：

   ```c
   Varyings OutlineRenderPassVertex(Attributes input)
   {
       Varyings output;
   
   #if defined(_NORMAL_FIXED)
       // 通过 TBN 矩阵计算平滑法线
       half3 normalOS = normalize(input.normalOS);
       half3 tangentOS = normalize(input.tangentOS.xyz);
       half3 bitangnetOS = normalize(cross(normalOS, tangentOS) * (input.tangentOS.w * GetOddNegativeScale()));
       half3 smoothNormalTS = UnpackNormalOctQuadEncode(input.smoothNormalTS);
       half3 smoothNormalOS = mul(smoothNormalTS, float3x3(tangentOS, bitangnetOS, normalOS));
       float3 normalWS = TransformObjectToWorldNormal(smoothNormalOS);
   #else
       half3 smoothNormalOS = normalize(input.normalOS);
       float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
   #endif
   
   #if defined(_USE_VERTEX_ALPHA)
       float outlineWidth = _OutlineWidth * input.color.a;
   #else
       float outlineWidth = _OutlineWidth;
   #endif
   
       output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
       // 通过物体到相机的距离控制描边粗细，可以用更好的曲线优化， 这里就不折腾了
       float3 offset = smoothNormalOS * min(0.2, output.positionCS.w * 0.5) * outlineWidth * 0.01;
       output.positionCS = TransformObjectToHClip(input.positionOS.xyz + offset);
       output.color = input.color;
       output.baseUV = input.baseUV;
       return output;
   }
   ```

   
   <div align=center>
   <img src="./assets/屏幕截图 2023-03-12 142202.png" alt="屏幕截图 2023-03-12 142202" style="zoom:50%;" />
   </div>

   <center>▲ 描边的最终效果图</center>

   

7. ##### 阴影 Shadow

   分为投射阴影和接受阴影。要投射阴影，需要写 ShadowCaster Pass。直接摘抄 URP 的 ShadowCaster，修改些变量，针对 Diffuse map A 通道的具体含义做修改，最终满足 SRP Batcher 即可。

   然后是接受阴影。一般来说，如果 Shadowmap 的精度不高，接受的阴影效果会大打折扣。不过，可以考虑基于深度的阴影来实现相似的效果。

   ```c
   float GetDepthShadow(float3 positionWS, float3 normalWS, float3 lightDir, float3 frontDir)
   {
   #if defined(_RECEIVE_DEPTH_SHADOWS)
       // 限制光照在一定范围内
       if (dot(frontDir, lightDir) < 0.86)
       {
           float3 rightDir = cross(frontDir, lightDir);
           float3 upDir = cross(rightDir, frontDir);
           lightDir = normalize(frontDir + 0.57 * upDir);
       }
   
       float stepLength = 0.001;
       float3 currPos = positionWS + stepLength * lightDir;
   
       // 做 Ray Marching 检验深度，看是否被遮挡
       UNITY_LOOP
       for (int i = 0; i < 30; i++)
       {
           float3 currPosVS = TransformWorldToView(currPos);
           float4 currPosCS = TransformWViewToHClip(currPosVS);
           float2 currUV = ComputeNormalizedDeviceCoordinates(currPosCS.xyz / currPosCS.w);
           
           float currDepthMap = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, currUV);
           float currObjDepth = LinearEyeDepth(currDepthMap, _ZBufferParams);
           float currEyeDepth = abs(currPosVS.z);
   
           UNITY_BRANCH
           if (step(0.0, currEyeDepth - currObjDepth))
           {
               return 0.0;
           }
   
           currPos += stepLength * lightDir;
       }
   #endif
   
       return 1.0;
   }
   ```

   由于效果仍然有些不稳定，最后没有加入这个效果。

   <div align=center>
   <img src="./assets/Project5.gif" alt="Project5" />
   </div>
   
   <center>▲ 用深度图实现脸部刘海阴影，未加入最终效果</center>



### 结果

加上天空盒。加上 Bloom，Tone Mapping 等后处理。写一些控制脚本。加入人物动作。看看最终效果吧。

<div align=center>
<img src="./assets/Project2.gif" alt="Project2" />
</div>

<div align=center>
<img src="./assets/Project3.gif" alt="Project3" />
</div>

<div align=center>
<img src="./assets/Project4.gif" alt="Project4" />
</div>



### 其他

- 关于 Shader GUI。由于参数众多，管理起来不便，于是实现一个可折叠的 GUI。
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-12 144250.png" alt="屏幕截图 2023-03-12 144250" style="zoom:50%;" />
  </div>

  <center>▲ 本项目 Shader 的属性列表</center>
  
  <div align=center>
  <img src="./assets/屏幕截图 2023-03-12 144320.png" alt="屏幕截图 2023-03-12 144320" style="zoom:50%;" />
  </div>

  <center>▲ 折叠后</center>

  写了 FoldoutDecorator 和 FoldEndDecorator （继承自 MaterialPropertyDrawer）实现 Shader 文件内折叠页的判断，并写了 FoldoutBaseShaderGUI （继承自 ShaderGUI）作为折叠页基类，提升扩展性。

  ```c
  // 以下是材质属性在 Shader 中的写法例子
  
  [Foldout(Setting)]  // 表示折叠页开始，和名字
  _DayTime("Time", Range(0, 24)) = 12
  [IntRange] _RampV1("Ramp Line of Mat1 (0.0~0.2)", Range(1, 5)) = 1
  [IntRange] _RampV2("Ramp Line of Mat2 (0.2~0.4)", Range(1, 5)) = 2
  [IntRange] _RampV3("Ramp Line of Mat3 (0.4~0.6)", Range(1, 5)) = 3
  [IntRange] _RampV4("Ramp Line of Mat4 (0.6~0.8)", Range(1, 5)) = 4
  [IntRange] _RampV5("Ramp Line of Mat5 (0.8~1.0)", Range(1, 5)) = 5
  [FoldEnd] // 折叠页结束标志，不支持折叠页嵌套
  
  [Foldout(Diffuse, _DIFFUSE_ON)]  // 有 Toggle 的折叠页，能设置宏 _DIFFUSE_ON 与否
  _Diffuse_Intensity("Diffuse Intensity", Range(0, 1)) = 1
  _Transition_Range("Transition Range", Range(0, 1)) = 0
  [FoldEnd]
  ```

  目前，该功能仍有些小 bug。
- 布料模拟使用 Magica Cloth 2，用起来挺方便的。 

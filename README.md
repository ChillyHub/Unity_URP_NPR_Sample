# Unity_URP_NPR_Sample

*URP are indeed used, but NPR not just included.*

> 记录项目所包含的内容，留下文档



#### 渲染篇

##### **仿原神的NPR角色渲染：** [ReadMe](./Documents/AvatarNPR.md)

<div align=center>
<img src="./Documents/assets/屏幕截图 2023-03-12 133132.png" alt="屏幕截图 2023-03-12 133132" />
</div>

<div align=center>
<img src="./Documents/assets/Project6.gif" alt="Project6" />
</div>


##### **PBR程序天空盒 （单极散射）：**[ReadMe](./Documents/SkyboxPBR.md)

<div align=center>
<img src="./Documents/assets/屏幕截图 2023-03-12 111819.png" alt="屏幕截图 2023-03-12 111819" />
</div>

<div align=center>
<img src="./Documents/assets/Project1.gif" alt="Project1" />
</div>


##### 镜面反射篇 ：[ReadMe](./Documents/Reflection.md)

目前实现了使用多 Pass 渲染的平面反射（前向渲染）

<img src="./Documents/assets/屏幕截图 2023-03-16 191407.png" />

SSR 屏幕空间反射 (延迟渲染)

<img src="./Documents/assets/屏幕截图 2023-05-28 000227.png" alt="屏幕截图 2023-05-28 000227" />



#### 工具篇

**自定义ShaderGUI，可以表示单层嵌套折叠项：**

<div align=center>
<img src="./Documents/assets/屏幕截图 2023-03-12 144250.png" alt="屏幕截图 2023-03-12 144250" style="zoom: 33%;" />
</div>


**类Blender相机的相机控制脚本：**

[Script: CameraSurroundPoint](./Assets/Scripts/Controller/CameraController/CameraSurroundPoint.cs)



#### 挖坑篇

勇士的冒险才刚刚开始，面对前方坑洼不断的路，勇士毅然决然，可他是否想过，这些坑洼是谁留下来的？



- 关于 NPR 大气天空盒的实现。目标之一是实现色彩调整的美术友好性，目标之二是更完善的功能，包括日月的表现，夜晚的星空，sdf 消隐的云朵以及对阳光的遮挡。
- 用多种不同的方法实现平面反射。包括多 Pass 渲染法，反射探针法，屏幕空间后处理的 SSR 法（用于延迟渲染）。
- 实现大面积草地的渲染。包括使用计算着色器优化，比如计算草的顶点运动，以及用计算着色器实现遮挡剔除。
- 水面渲染。除了对深度贴图的利用，还考虑使用计算着色器实现水面波浪，以及 SSR 特性的加入。
- DOTS。利用 ECS 的特性做大量物体的运算渲染。
- 未完待续 ……

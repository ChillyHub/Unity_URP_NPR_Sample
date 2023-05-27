using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PipelineRenderer
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/Screen Space Reflection", typeof(UniversalRenderPipeline))]
    public class ScreenSpaceReflection : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false, true);

        public ClampedFloatParameter stopIndex = new ClampedFloatParameter(100.0f, 0.0f, 1000.0f);
        public ClampedFloatParameter maxLoop = new ClampedFloatParameter(2000.0f, 0.0f, 2000.0f);
        public ClampedFloatParameter thickness = new ClampedFloatParameter(0.2f, 0.0f, 1.0f);
        public ClampedIntParameter maxStepSize = new ClampedIntParameter(16, 1, 256);

        public bool IsActive() => enable.value;

        public bool IsTileCompatible() => false;
    }
}
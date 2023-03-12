using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PipelineRenderer
{
    [Serializable, VolumeComponentMenuForRenderPipeline("Post-processing/Screen Space Rim", 
         typeof(UniversalRenderPipeline))]
    public class ScreenSpaceRim : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        public ClampedFloatParameter bias = new ClampedFloatParameter(2.0f, 0.0f, 40.0f);

        public ClampedFloatParameter threshold = new ClampedFloatParameter(0.1f, 0.1f, 1.0f);

        public bool IsActive() => active && intensity.value > 0.0f;

        public bool IsTileCompatible() => false;
    }
}
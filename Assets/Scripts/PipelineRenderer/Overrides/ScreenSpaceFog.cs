using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PipelineRenderer
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/Screen Space Fog", typeof(UniversalRenderPipeline))]
    public class ScreenSpaceFog : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false, true);

        [Header("Color Setting")] 
        public ColorParameter lightColor = new ColorParameter(Color.white, true, true, false);
        public ClampedFloatParameter exposure = new ClampedFloatParameter(0.476f, 0.0f, 2.0f);

        [Header("Geometry Setting")]
        public ClampedFloatParameter thickness = new ClampedFloatParameter(25.0f, 0.0f, 1000.0f);
        public FloatParameter radius = new FloatParameter(6400.0f);
        public ClampedFloatParameter radiusThicknessScale = new ClampedFloatParameter(1.0f, 0.0f, 10.0f);

        [Header("Physic Setting")]
        public ClampedFloatParameter rayleighFac = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter mieFac = new ClampedFloatParameter(0.01f, 0.0f, 1.0f);
        public ClampedFloatParameter mieCoefficientG = new ClampedFloatParameter(0.133f, 0.75f, 0.9999f);

        [Header("Performance Setting")] 
        public ClampedIntParameter sampleCounts = new ClampedIntParameter(30, 0, 60);
        
        public bool IsActive() => enable.value;

        public bool IsTileCompatible() => false;
    }
}
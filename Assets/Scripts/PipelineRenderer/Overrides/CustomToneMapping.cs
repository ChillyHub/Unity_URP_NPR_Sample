using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace PipelineRenderer
{
    [Serializable]
    [VolumeComponentMenuForRenderPipeline("Post-processing/Custom Tone Mapping", typeof(UniversalRenderPipeline))]
    public class CustomToneMapping : VolumeComponent, IPostProcessComponent
    {
        [Serializable]
        public enum Mapper
        {
            None,
            GranTurismo
        }

        [Serializable]
        public class GranTurismoData
        {
            [Header("Gran Turismo Setting")]
            public ClampedFloatParameter maximumBrightness = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
            public ClampedFloatParameter slope = new ClampedFloatParameter(1.0f, 0.0f, 5.0f);
            public ClampedFloatParameter linearSectionStart = new ClampedFloatParameter(0.22f, 0.0f, 1.0f);
            public ClampedFloatParameter linearSectionLength = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);
            public ClampedFloatParameter blackTightness = new ClampedFloatParameter(1.33f, 0.0f, 3.0f);
            public ClampedFloatParameter darknessValue = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
            
            // _P => Maximum brightness
            // _a => Slope
            // _m => Linear section start
            // _l => Linear section length
            // _c => Black tightness
            // _b => Darkness value
        }

        public MapperParameter mode = new MapperParameter(Mapper.None, true);
        public GranTurismoData granTurismoData = new GranTurismoData();
        
        public bool IsActive() => mode.value != Mapper.None;

        public bool IsTileCompatible() => false;
    }

    [Serializable]
    public sealed class MapperParameter : VolumeParameter<CustomToneMapping.Mapper>
    {
        public MapperParameter(CustomToneMapping.Mapper value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
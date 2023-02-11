using System;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using Object = System.Object;

namespace UnityEditor
{
    public class AvatarMaterialGUI : FoldoutBaseShaderGUI
    {
        #region Properties

        public enum MaterialType
        {
            Body,
            Hair,
            Face
        }

        public enum RenderMode
        {
            Opaque,
            Transparent,
            AlphaTest,
            Custom
        }

        private enum ShadowMode
        {
            On,
            Clip,
            Dither,
            Off
        }

        private MaterialType AvatarMaterial
        {
            set
            {
                SetKeyword("_IS_BODY", value == MaterialType.Body);
                SetKeyword("_IS_HAIR", value == MaterialType.Hair);
                SetKeyword("_IS_FACE", value == MaterialType.Face);
                _materialType = value;
            }
            get => _materialType;
        }

        private MaterialType _materialType = MaterialType.Body;

        private RenderMode AvatarRenderMode
        {
            set
            {
                string[] propName =
                {
                    "_Clipping", "_PreMulAlpha", "_SrcBlend", "_DstBlend", "_ZWrite", "_Shadows"
                };

                if (value == RenderMode.Opaque)
                {
                    Clipping = false;
                    PremultiplyAlpha = false;
                    SrcBlend = BlendMode.One;
                    DstBlend = BlendMode.Zero;
                    ZWrite = true;
                    RenderQueue = RenderQueue.Geometry;
                    AvatarShadowMode = ShadowMode.On;

                    SetRenderModeEnableProp(propName, false);
                }
                else if (value == RenderMode.Transparent)
                {
                    Clipping = false;
                    PremultiplyAlpha = false;
                    SrcBlend = BlendMode.SrcAlpha;
                    DstBlend = BlendMode.OneMinusSrcAlpha;
                    ZWrite = false;
                    RenderQueue = RenderQueue.Transparent;
                    AvatarShadowMode = ShadowMode.Dither;

                    if (FindProperty("_PreMulAlpha", _materialProperties, false) != null)
                    {
                        PremultiplyAlpha = true;
                    }

                    SetRenderModeEnableProp(propName, false);
                }
                else if (value == RenderMode.AlphaTest)
                {
                    Clipping = true;
                    PremultiplyAlpha = false;
                    SrcBlend = BlendMode.One;
                    DstBlend = BlendMode.Zero;
                    ZWrite = true;
                    RenderQueue = RenderQueue.AlphaTest;
                    AvatarShadowMode = ShadowMode.Clip;

                    SetRenderModeEnableProp(propName, false);
                }
                else if (value == RenderMode.Custom)
                {
                    SetRenderModeEnableProp(propName, true);
                }

                SetKeyword("_IS_OPAQUE", value == RenderMode.Opaque);
                SetKeyword("_IS_TRANSPARENT", value == RenderMode.Transparent || value == RenderMode.AlphaTest);
                _avatarRenderMode = value;
            }
            get => _avatarRenderMode;
        }

        private RenderMode _avatarRenderMode = RenderMode.Opaque;

        private ShadowMode AvatarShadowMode
        {
            set
            {
                if (SetProperty("_Shadows", (float)value))
                {
                    SetKeyword("_SHADOWS_CLIP", value == ShadowMode.Clip);
                    SetKeyword("_SHADOWS_DITHER", value == ShadowMode.Dither);
                }
            }
        }

        private string[] _materialName =
        {
            "Body", "Hair", "Face"
        };

        private BlendMode SrcBlend
        {
            set => SetProperty("_SrcBlend", (float)value);
        }

        private BlendMode DstBlend
        {
            set => SetProperty("_DstBlend", (float)value);
        }

        private bool ZWrite
        {
            set => SetProperty("_ZWrite", value ? 1.0f : 0.0f);
        }

        private bool Clipping
        {
            set => SetProperty("_Clipping", "_CLIPPING", value);
        }

        private bool PremultiplyAlpha
        {
            set => SetProperty("_PreMulAlpha", "_PREMULTIPLY_ALPHA", value);
        }

        private RenderQueue RenderQueue
        {
            set
            {
                foreach (Material m in _materials)
                {
                    m.renderQueue = (int)value;
                }
            }
        }

        #endregion

        #region Data

        public Dictionary<string, int> PropertiesIndex { get; set; }

        private Object[] _materials;

        private readonly string _keyPrefix = "Assets:Editor:GUI:AvatarMaterial:";

        #endregion

        protected override void Init(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _materials = materialEditor.targets;

            if (_isChanged)
            {
                PropertiesIndex = new Dictionary<string, int>();
                for (int i = 0; i < properties.Length; i++)
                {
                    PropertiesIndex[properties[i].name] = i;
                }
            }

            AvatarMaterial = (MaterialType)GetEditorPrefs(
                _keyPrefix + (materialEditor.target as Material).name + ":Material Type");
            AvatarRenderMode = (RenderMode)GetEditorPrefs(
                _keyPrefix + (materialEditor.target as Material).name + ":Render Mode");
            
            for (int i = 0; i < properties.Length; i++)
            {
                if (AvatarMaterial != MaterialType.Face && properties[i].name == "_FaceLightMap")
                {
                    IsExpanded[i] = false;
                }
            }
        }

        protected override void BeforeBaseGUI()
        {
            AvatarMaterial = (MaterialType)EditorGUILayout.EnumPopup("Material Type", AvatarMaterial);
            AvatarRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", AvatarRenderMode);
            SetEditorPrefs(
                _keyPrefix + (_materialEditor.target as Material).name + ":Material Type", (int)AvatarMaterial);
            SetEditorPrefs(
                _keyPrefix + (_materialEditor.target as Material).name + ":Render Mode", (int)AvatarRenderMode);
            
            _materialEditor.SetDefaultGUIWidths();
            EditorGUILayout.Space();
        }

        protected override void AfterBaseGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            _materialEditor.EnableInstancingField();
            _materialEditor.DoubleSidedGIField();
        }

        uint GetEditorPrefs(string key)
        {
            if (EditorPrefs.HasKey(key))
            {
                return (uint)EditorPrefs.GetInt(key);
            }

            return 0;
        }
        
        void SetEditorPrefs(string key, int value)
        {
            EditorPrefs.SetInt(key, value);
        }

        void SetRenderModeEnableProp(string[] propName, bool enable)
        {
            foreach (var s in propName)
            {
                if (PropertiesIndex.ContainsKey(s) && IsActive != null)
                {
                    IsActive[PropertiesIndex[s]] = enable;
                }
            }

            if (PropertiesIndex.ContainsKey("RenderQueue"))
            {
                PropertiesIndex["RenderQueue"] = Convert.ToInt32(enable);
            }
        }
        
        #region Utility

        void SetKeyword(string keyword, bool enabled)
        {
            if (enabled)
            {
                foreach (Material m in _materials)
                {
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in _materials)
                {
                    m.DisableKeyword(keyword);
                }
            }
        }
        
        bool SetProperty(string name, float value)
        {
            MaterialProperty property = FindProperty(name, _materialProperties, false);
            if (property != null)
            {
                property.floatValue = value;
                return true;
            }

            return false;
        } 

        void SetProperty(string name, string keyword, bool value)
        {
            if (SetProperty(name, value ? 1.0f : 0.0f))
            {
                SetKeyword(keyword, value);
            }
        }

        #endregion
    }
}
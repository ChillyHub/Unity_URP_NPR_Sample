using System;
using System.Collections.Generic;
using Codice.Client.BaseCommands;using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

public static class ShaderToggleMap
{
    public static Dictionary<string, bool[]> FoldToggleOn = new Dictionary<string, bool[]>();

    public static Dictionary<string, AvatarShaderGUI.MaterialType> MaterialTypes =
        new Dictionary<string, AvatarShaderGUI.MaterialType>();

    public static Dictionary<string, AvatarShaderGUI.RenderMode> RenderModes =
        new Dictionary<string, AvatarShaderGUI.RenderMode>();
}

public class AvatarShaderGUI : ShaderGUI
{
    private MaterialEditor _editor;
    private MaterialProperty[] _properties;
    private Object[] _materials;

    #region GUISetting

    public float FoldHeight
    {
        get => _foldHeight;
        set => _foldHeight = value;
    }
    private float _foldHeight = 20.0f;

    #endregion

    # region GUIState

    public int CurrPropIndex
    {
        get => _currPropIndex;
        set => _currPropIndex = value;
    }
    private int _currPropIndex = 0;

    public bool CurrEnablePresent
    {
        get => _currEnablePresent;
        set => _currEnablePresent = value;
    }
    private bool _currEnablePresent = true;
    
    public bool CurrInFold
    {
        get => _currInFold;
        set => _currInFold = value;
    }
    private bool _currInFold = false;
    
    public bool[] EnablePresent
    {
        get => _enablePresent;
        set => _enablePresent = value;
    }
    private bool[] _enablePresent;

    public bool[] EnableEditor
    {
        get => _enableEditor;
        set => _enableEditor = value;
    }
    private bool[] _enableEditor;
    
    public bool[] InFold
    {
        get => _inFold;
        set => _inFold = value;
    }
    private bool[] _inFold;

    public bool[] FoldToggleOn
    {
        get => _foldToggleOn;
        set => _foldToggleOn = value;
    }
    private static bool[] _foldToggleOn;

    public Dictionary<string, int> PropertiesIndex
    {
        get => _propertiesIndex;
        set => _propertiesIndex = value;
    }
    private Dictionary<string, int> _propertiesIndex;

    public BeginFoldDecorator[] BeginFoldDecorators
    {
        get => _beginFoldDecorators;
        set => _beginFoldDecorators = value;
    }
    private BeginFoldDecorator[] _beginFoldDecorators;

    public EndFoldDecorator[] EndFoldDecorators
    {
        get => _endFoldDecorators;
        set => _endFoldDecorators = value;
    }
    private EndFoldDecorator[] _endFoldDecorators;

    #endregion

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
                
                if (FindProperty("_PreMulAlpha", _properties, false) != null)
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

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        Init(editor, properties);
        
        DrawMaterialSwitchField();
        DrawRenderModeSwitchField();

        editor.SetDefaultGUIWidths();
        CurrEnablePresent = true;
        CurrInFold = false;
        
        EditorGUILayout.Space();
        
        DrawProperties();

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
        {
            EditorGUI.BeginDisabledGroup(PropertiesIndex["RenderQueue"] == 0);
            editor.RenderQueueField();
            EditorGUI.EndDisabledGroup();
        }
        editor.EnableInstancingField();
        editor.DoubleSidedGIField();
    }

    void Init(MaterialEditor editor, MaterialProperty[] properties)
    {
        _editor = editor;
        _properties = properties;
        _materials = editor.targets;

        if (_enablePresent == null || _enablePresent.Length != properties.Length)
        {
            _enablePresent = new bool[_properties.Length];
            _enableEditor = new bool[_properties.Length];
            _inFold = new bool[_properties.Length];
            _propertiesIndex = new Dictionary<string, int>();
            _beginFoldDecorators = new BeginFoldDecorator[_properties.Length];
            _endFoldDecorators = new EndFoldDecorator[_properties.Length];
            for (int i = 0; i < _properties.Length; i++)
            {
                EnablePresent[i] = true;
                EnableEditor[i] = true;
                InFold[i] = false;
            }
            
            _propertiesIndex.Add("RenderQueue", 1);
            
            if (ShaderToggleMap.FoldToggleOn.ContainsKey(editor.target.name))
            {
                _foldToggleOn = ShaderToggleMap.FoldToggleOn[editor.target.name];
            }
            else
            {
                _foldToggleOn = new bool[_properties.Length];
                for (int i = 0; i < _properties.Length; i++)
                {
                    FoldToggleOn[i] = true;
                }

                ShaderToggleMap.FoldToggleOn.Add(editor.target.name, FoldToggleOn);
            }

            if (ShaderToggleMap.MaterialTypes.ContainsKey(editor.target.name))
            {
                AvatarMaterial = ShaderToggleMap.MaterialTypes[editor.target.name];
            }
            else
            {
                AvatarMaterial = MaterialType.Body;
                ShaderToggleMap.MaterialTypes.Add(editor.target.name, AvatarMaterial);
            }
            
            if (ShaderToggleMap.RenderModes.ContainsKey(editor.target.name))
            {
                AvatarRenderMode = ShaderToggleMap.RenderModes[editor.target.name];
            }
            else
            {
                AvatarRenderMode = RenderMode.Opaque;
                ShaderToggleMap.RenderModes.Add(editor.target.name, AvatarRenderMode);
            }
        }
    }

    void DrawMaterialTypeButton()
    {
        AvatarMaterial = (MaterialType)GUILayout.SelectionGrid(
            Convert.ToInt32(AvatarMaterial), _materialName, _materialName.Length);
    }

    void DrawMaterialSwitchField()
    {
        AvatarMaterial = (MaterialType)EditorGUILayout.EnumPopup("Material Type", AvatarMaterial);
        ShaderToggleMap.MaterialTypes[_editor.target.name] = AvatarMaterial;

    }

    void DrawRenderModeSwitchField()
    {
        AvatarRenderMode = (RenderMode)EditorGUILayout.EnumPopup("Render Mode", AvatarRenderMode);
        ShaderToggleMap.RenderModes[_editor.target.name] = AvatarRenderMode;
    }

    void DrawProperties()
    {
        for (int i = 0; i < _properties.Length; i++)
        {
            CurrPropIndex = i;
            if ((!InFold[i] || InFold[i] && EnablePresent[i]) && 
                (_properties[i].flags & MaterialProperty.PropFlags.HideInInspector) 
                != MaterialProperty.PropFlags.HideInInspector)
            {
                if (_properties[i].name != "_FaceLightMap" || AvatarMaterial == MaterialType.Face)
                {
                    EditorGUI.BeginDisabledGroup(!EnableEditor[i]);

                    _editor.ShaderProperty(
                        EditorGUILayout.GetControlRect(
                            true,
                            _editor.GetPropertyHeight(_properties[i], _properties[i].displayName),
                            EditorStyles.layerMaskField), 
                        _properties[i], _properties[i].displayName);

                    EditorGUI.EndDisabledGroup();
                }

                EnablePresent[i] = CurrEnablePresent;
                InFold[i] = CurrInFold;
                
                if (!PropertiesIndex.ContainsKey(_properties[i].name))
                {
                    PropertiesIndex.Add(_properties[i].name, i);
                }
            }
            else
            {
                if (EndFoldDecorators[i] != null)
                {
                    EndFoldDecorators[i].OnGUI(
                        EditorGUILayout.GetControlRect(
                            true, 
                            EndFoldDecorators[i].GetPropertyHeight(_properties[i], "", _editor) - 
                            EditorGUIUtility.standardVerticalSpacing, 
                            EditorStyles.layerMaskField),
                        _properties[i], "", _editor);
                }
                if (BeginFoldDecorators[i] != null)
                {
                    BeginFoldDecorators[i].OnGUI(
                        EditorGUILayout.GetControlRect(
                            true, 
                            BeginFoldDecorators[i].GetPropertyHeight(_properties[i], "", _editor) - 
                            EditorGUIUtility.standardVerticalSpacing, 
                            EditorStyles.layerMaskField),
                        _properties[i], "", _editor);
                }

                EnablePresent[i] = CurrEnablePresent;
                InFold[i] = CurrInFold;
                
                if (!PropertiesIndex.ContainsKey(_properties[i].name))
                {
                    PropertiesIndex.Add(_properties[i].name, i);
                }
            }
        }
    }

    void SetRenderModeEnableProp(string[] propName, bool enable)
    {
        foreach (var s in propName)
        {
            if (PropertiesIndex.ContainsKey(s) && EnableEditor != null)
            {
                EnableEditor[PropertiesIndex[s]] = enable;
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
        MaterialProperty property = FindProperty(name, _properties, false);
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
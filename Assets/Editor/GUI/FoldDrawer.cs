using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Internal;

public class BeginFoldDecorator : MaterialPropertyDrawer
{
    private AvatarShaderGUI _avatarShaderGUI;
    private MaterialProperty _materialProperty;

    private string _foldName;
    private string _toggleName;
    
    private bool _foldOpen = true;
    private bool _foldToggleDraw = false;
    private bool _foldEditor = true;
    private int _initFoldEditor = -1;
    
    public BeginFoldDecorator() : this("Fold") {}
    public BeginFoldDecorator(string foldName) { _foldName = foldName; }

    public BeginFoldDecorator(string foldName, float toggleOn)
    {
        _foldName = foldName; 
        _foldToggleDraw = Convert.ToBoolean(toggleOn);
        if (_foldToggleDraw)
        {
            _toggleName = "_" + _foldName.ToUpperInvariant().Replace(' ', '_') + "_ON";
        }
    }
    
    public BeginFoldDecorator(string foldName, string toggleName)
    {
        _foldName = foldName;
        _toggleName = toggleName;
        _foldToggleDraw = true;
    }
    
    public BeginFoldDecorator(string foldName, string toggleName, float value)
    {
        _foldName = foldName;
        _toggleName = toggleName;
        _foldToggleDraw = true;
        _initFoldEditor = Convert.ToInt32(value);
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        _avatarShaderGUI = editor.customShaderGUI as AvatarShaderGUI;

        if (_avatarShaderGUI != null)
        {
            return _avatarShaderGUI.FoldHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        return 20.0f + EditorGUIUtility.standardVerticalSpacing;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        _avatarShaderGUI = editor.customShaderGUI as AvatarShaderGUI;
        _materialProperty = prop;

        if (_avatarShaderGUI != null)
        {
            if (!_avatarShaderGUI.EnableEditor[_avatarShaderGUI.CurrPropIndex])
            {
                EditorGUI.EndDisabledGroup();
                if (_avatarShaderGUI.EnablePresent[_avatarShaderGUI.CurrPropIndex])
                {
                    EditorGUI.EndDisabledGroup();
                    FoldGUIDraw(position);
                    EditorGUI.BeginDisabledGroup(true);
                }
            }
            FoldGUIDraw(position);

            _avatarShaderGUI.CurrInFold = true;
            _avatarShaderGUI.CurrEnablePresent = _foldOpen;
            _avatarShaderGUI.BeginFoldDecorators[_avatarShaderGUI.CurrPropIndex] = this;
        }
    }

    private void FoldGUIDraw(Rect position)
    {
        GUIStyle style = new GUIStyle(EditorStyles.foldoutHeader);
        style.font = EditorStyles.boldLabel.font;
        style.fontStyle = EditorStyles.boldLabel.fontStyle;
        style.fontSize = EditorStyles.boldLabel.fontSize;
        style.fixedHeight = _avatarShaderGUI.FoldHeight;

        _foldOpen = DrawFold(position, _foldOpen, _foldName, style);
    }

    public bool DrawFold(
        Rect position, 
        bool foldout,
        string content,
        [DefaultValue("EditorStyles.foldoutHeader")] GUIStyle style = null)
    {
        return DrawFold(position, foldout, new GUIContent(content), style);
    }
    
    public bool DrawFold(
        Rect position,
        bool foldout,
        GUIContent content,
        [DefaultValue("EditorStyles.foldoutHeader")] GUIStyle style = null)
    {
        if (EditorGUIUtility.hierarchyMode)
        {
            position.xMin -= (float) EditorStyles.inspectorDefaultMargins.padding.left;
            position.xMax += (float) EditorStyles.inspectorDefaultMargins.padding.right;
        }
        if (style == null)
        {   
            style = EditorStyles.foldoutHeader;
        }
        Rect position1 = new Rect()
        {
            x = (float) ((double) position.xMax - (double) style.padding.right - 64.0),
            y = position.y + (float) style.padding.top,
            size = Vector2.one * 16f
        };
        bool isHover = position.Contains(UnityEngine.Event.current.mousePosition);
        bool isActive = isHover && UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown && 
                        UnityEngine.Event.current.button == 0;
        bool isHover1 = position1.Contains(UnityEngine.Event.current.mousePosition);
        bool isActive1 = isHover1 && UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown && 
                         UnityEngine.Event.current.button == 0;
        //int controlId = GUIUtility.GetControlID(EditorGUI.s_FoldoutHeaderHash, FocusType.Keyboard, position);
        if (UnityEngine.Event.current.type == UnityEngine.EventType.KeyDown)
            // && GUIUtility.keyboardControl == controlId)
        {
            KeyCode keyCode = UnityEngine.Event.current.keyCode;
            if (keyCode == KeyCode.LeftArrow & foldout || keyCode == KeyCode.RightArrow && !foldout)
            {
                foldout = !foldout;
                GUI.changed = true;
                UnityEngine.Event.current.Use();
            }
        }
        else if (UnityEngine.Event.current.type == UnityEngine.EventType.MouseDown)
        {
            if (position.Contains(UnityEngine.Event.current.mousePosition) && 
                !(_foldToggleDraw && position1.Contains(UnityEngine.Event.current.mousePosition)))
            {
                foldout = !foldout;
                GUI.changed = true;
                UnityEngine.Event.current.Use();
            }
        }
        else
        {
            if (UnityEngine.Event.current.type == EventType.Repaint)
            {
                Color color = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
                style.Draw(position, content, false, isActive, foldout, false);
                GUI.backgroundColor = color;
            }
        }
        if (_foldToggleDraw)
        {
            EditorGUI.BeginChangeCheck();
            if (_materialProperty.hasMixedValue)
            {
                _foldEditor = GUI.Toggle(position1, false, "", new GUIStyle("ToggleMixed"));
            }
            else
            {
                if (_initFoldEditor != 2)
                {
                    if (_initFoldEditor == -1)
                    {
                        _foldEditor = true;
                        _initFoldEditor = 2;
                    }
                    else
                    {
                        _foldEditor = Convert.ToBoolean(_initFoldEditor);
                        _initFoldEditor = 2;
                    }
                    
                    SetKeyword(_toggleName, _foldEditor);
                    _foldEditor = GUI.Toggle(position1, _foldEditor, "");
                    _avatarShaderGUI.FoldToggleOn[_avatarShaderGUI.CurrPropIndex] = _foldEditor;
                    EditorGUI.EndChangeCheck();
                    return foldout;
                }
                _foldEditor = _avatarShaderGUI.FoldToggleOn[_avatarShaderGUI.CurrPropIndex];
                _foldEditor = GUI.Toggle(position1, _foldEditor, "");
            }
            if (EditorGUI.EndChangeCheck())
            {
                SetKeyword(_toggleName, _foldEditor);
                _avatarShaderGUI.FoldToggleOn[_avatarShaderGUI.CurrPropIndex] = _foldEditor;
            }
        }
        return foldout;
    }
    
    #region Utility

    void SetKeyword(string keyword, bool enabled)
    {
        if (_materialProperty != null)
        {
            if (enabled)
            {
                foreach (Material m in _materialProperty.targets)
                {
                    m.EnableKeyword(keyword);
                }
            }
            else
            {
                foreach (Material m in _materialProperty.targets)
                {
                    m.DisableKeyword(keyword);
                }
            }
        }
    }
        
    bool SetProperty(float value)
    {
        if (_materialProperty != null)
        {
            _materialProperty.floatValue = value;
            return true;
        }

        return false;
    } 

    void SetProperty(string keyword, bool value)
    {
        if (SetProperty(value ? 1.0f : 0.0f))
        {
            SetKeyword(keyword, value);
        }
    }

    #endregion
}

public class EndFoldDecorator : MaterialPropertyDrawer
{
    private AvatarShaderGUI _avatarShaderGUI;
    private MaterialProperty _materialProperty;

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        _avatarShaderGUI = editor.customShaderGUI as AvatarShaderGUI;

        if (_avatarShaderGUI != null)
        {
            if (_avatarShaderGUI.CurrInFold && _avatarShaderGUI.CurrEnablePresent)
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }

        return 0.0f;
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        _avatarShaderGUI = editor.customShaderGUI as AvatarShaderGUI;
        
        if (_avatarShaderGUI != null)
        {
            _avatarShaderGUI.CurrInFold = false;
            _avatarShaderGUI.CurrEnablePresent = true;
            _avatarShaderGUI.EndFoldDecorators[_avatarShaderGUI.CurrPropIndex] = this;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor
{
    public static class MaterialHeaderScopes
    {
        private static readonly Stack<MaterialHeaderScope> _materialHeaderScopes = new Stack<MaterialHeaderScope>();
        private static bool _currExpanded = true;
        private static bool _currActive = true;

        public static void DrawBegin(Rect rect, string title, int foldoutCount, MaterialEditor editor, 
            bool isToggle = false)
        {
            uint expandable = Convert.ToUInt32(1 << (foldoutCount - 1));
            var scope = new MaterialHeaderScope(rect, title, expandable, editor, false, isToggle);
            
            _materialHeaderScopes.Push(scope);
            _currExpanded = scope.expanded;
            _currActive = editor.IsAreaActive(expandable);
        }

        public static void DrawEnd()
        {
            if (_materialHeaderScopes.TryPop(out MaterialHeaderScope scope))
            {
                ((IDisposable)scope).Dispose();

                _currExpanded = true;
                _currActive = true;
            }
        }

        public static void Clear()
        {
            _materialHeaderScopes.Clear();
        }

        public static bool IsTopExpanded()
        {
            return _currExpanded;
        }

        public static bool IsCurrActive()
        {
            return _currActive;
        }
    }
    
    public class FoldoutDecorator : MaterialPropertyDrawer
    {
        private FoldoutBaseShaderGUI _foldoutBaseGUI;
        private MaterialProperty _property;

        private readonly string _foldoutName;
        private readonly string _toggleName;
        private readonly bool _isToggle = false;

        public FoldoutDecorator(string foldoutName)
        {
            _foldoutName = foldoutName;
        }
        
        public FoldoutDecorator(string foldoutName, string toggleName)
        {
            _foldoutName = foldoutName;
            _toggleName = toggleName;
            _isToggle = true;
        }

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 18.0f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            _foldoutBaseGUI = editor.customShaderGUI as FoldoutBaseShaderGUI;

            if (_foldoutBaseGUI != null)
            {
                _property = prop;
                
                _foldoutBaseGUI.FoldoutCount++;
                _foldoutBaseGUI.IsBegin[_foldoutBaseGUI.CurrIndex] = this;
                
                EditorGUI.BeginChangeCheck();
                if (!_foldoutBaseGUI.IsActive[_foldoutBaseGUI.CurrIndex])
                {
                    EditorGUI.EndDisabledGroup();
                    if (_foldoutBaseGUI.IsExpanded[_foldoutBaseGUI.CurrIndex])
                    {
                        EditorGUI.EndDisabledGroup();
                        MaterialHeaderScopes.DrawBegin(
                            position, _foldoutName, _foldoutBaseGUI.FoldoutCount, editor, _isToggle);
                        EditorGUI.BeginDisabledGroup(true);
                    }
                    else
                    {
                        MaterialHeaderScopes.DrawBegin(
                            position, _foldoutName, _foldoutBaseGUI.FoldoutCount, editor, _isToggle);
                    }
                }
                else
                {
                    MaterialHeaderScopes.DrawBegin(
                        position, _foldoutName, _foldoutBaseGUI.FoldoutCount, editor, _isToggle);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    SetKeyword(_toggleName, MaterialHeaderScopes.IsCurrActive());
                }
            }
        }
        
        void SetKeyword(string keyword, bool enabled)
        {
            if (_property != null)
            {
                if (enabled)
                {
                    foreach (Material m in _property.targets)
                    {
                        m.EnableKeyword(keyword);
                    }
                }
                else
                {
                    foreach (Material m in _property.targets)
                    {
                        m.DisableKeyword(keyword);
                    }
                }
            }
        }
    }

    public class FoldEndDecorator : MaterialPropertyDrawer
    {
        private FoldoutBaseShaderGUI _foldoutBaseGUI;

        public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
        {
            return 0.0f;
        }

        public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
        {
            _foldoutBaseGUI = editor.customShaderGUI as FoldoutBaseShaderGUI;

            if (_foldoutBaseGUI != null)
            {
                _foldoutBaseGUI.IsEnd[_foldoutBaseGUI.CurrIndex] = this;
                
                MaterialHeaderScopes.DrawEnd();
            }
        }
    }
}
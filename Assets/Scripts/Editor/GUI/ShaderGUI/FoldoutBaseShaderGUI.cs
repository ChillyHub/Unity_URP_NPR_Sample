using System;
using UnityEditor.Rendering;
using UnityEngine;

namespace UnityEditor
{
    public class FoldoutBaseShaderGUI : ShaderGUI
    {
        public FoldoutDecorator[] IsBegin;
        public FoldEndDecorator[] IsEnd;
        public bool[] IsExpanded;
        public bool[] IsActive;
        public int FoldoutCount = 0;
        public int CurrIndex = 0;
        
        public bool IsChanged { get => _isChanged; }

        protected MaterialEditor _materialEditor;
        protected MaterialProperty[] _materialProperties;
        
        protected bool _isChanged = true;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (materialEditor == null)
            {
                throw new ArgumentNullException("materialEditor");
            }

            BaseInit(materialEditor, properties);
            Init(materialEditor, properties);
            
            BeforeBaseGUI();
            OnBaseGUI(properties);
            AfterBaseGUI();
        }

        protected virtual void Init(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            
        }

        protected virtual void BeforeBaseGUI()
        {
            
        }

        protected virtual void AfterBaseGUI()
        {
            
        }

        void BaseInit(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            if (_materialProperties == null)
            {
                _isChanged = true;
                
                DataAllocate(materialEditor, properties);
            }
            else if (_materialProperties.Length != properties.Length)
            {
                _isChanged = true;
                
                DataAllocate(materialEditor, properties);
            }
            else
            {
                var len = Math.Min(_materialProperties.Length, properties.Length);
                for (int i = 0; i < len; i++)
                {
                    if (_materialProperties[i].name != properties[i].name)
                    {
                        _isChanged = true;
                        break;
                    }
                }

                if (_isChanged)
                {
                    DataAllocate(materialEditor, properties);
                }
            }

            if (_isChanged)
            {
                for (int i = 0; i < _materialProperties.Length; i++)
                {
                    CurrIndex = i;
                    _materialEditor.ShaderProperty(properties[i], properties[i].displayName);
                    IsExpanded[i] = MaterialHeaderScopes.IsTopExpanded();
                    IsActive[i] = MaterialHeaderScopes.IsCurrActive();
                }
                FoldoutCount = 0;
            }
        }

        void OnBaseGUI(MaterialProperty[] properties)
        {
            _materialEditor.SetDefaultGUIWidths();
            
            for (int i = 0; i < _materialProperties.Length; i++)
            {
                CurrIndex = i;

                if (IsExpanded[i] && (properties[i].flags & MaterialProperty.PropFlags.HideInInspector) == 0)
                {
                    float height = _materialEditor.GetPropertyHeight(
                        _materialProperties[i], _materialProperties[i].displayName);

                    if (i > 0 && IsExpanded[i - 1])
                    {
                        if (IsEnd[i] != null)
                        {
                            EditorGUILayout.Space(12.0f);
                        }
                        EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }
                    Rect rect = GUILayoutUtility.GetRect(height, height);
                    
                    EditorGUI.BeginDisabledGroup(!IsActive[i]);
                    _materialEditor.ShaderProperty(rect, properties[i], properties[i].displayName);
                    EditorGUI.EndDisabledGroup();
                    
                    IsExpanded[i] = MaterialHeaderScopes.IsTopExpanded();
                    IsActive[i] = MaterialHeaderScopes.IsCurrActive();
                }
                else
                {
                    float height = 0.0f;
                    if (IsEnd[i] != null)
                    {
                        height += IsEnd[i].GetPropertyHeight(_materialProperties[i], "", _materialEditor);
                    }
                    if (IsBegin[i] != null)
                    {
                        height += IsBegin[i].GetPropertyHeight(_materialProperties[i], "", _materialEditor);
                    }
                    
                    if (i > 0 && IsExpanded[i - 1])
                    {
                        if (IsEnd[i] != null)
                        {
                            EditorGUILayout.Space(12.0f);
                        }
                        EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
                    }
                    Rect rect = GUILayoutUtility.GetRect(height, height);

                    if (IsEnd[i] != null)
                    {
                        var end = IsEnd[i];
                        end.OnGUI(rect, _materialProperties[i], "", _materialEditor);
                    }
                    if (IsBegin[i] != null)
                    {
                        var begin = IsBegin[i];
                        begin.OnGUI(rect, _materialProperties[i], "", _materialEditor);
                    }
                    IsExpanded[i] = MaterialHeaderScopes.IsTopExpanded();
                    IsActive[i] = MaterialHeaderScopes.IsCurrActive();
                }
            }
            FoldoutCount = 0;
            
            _isChanged = false;
            MaterialHeaderScopes.Clear();
        }
        
        void DataAllocate(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _materialEditor = materialEditor;
            _materialProperties = properties;

            IsBegin = new FoldoutDecorator[properties.Length];
            IsEnd = new FoldEndDecorator[properties.Length];
            IsExpanded = new bool[properties.Length];
            IsActive = new bool[properties.Length];
        }
    }
}

using System;
using UnityEngine;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// Create a toggleable header for material UI, must be used within a scope.
    /// <example>Example:
    /// <code>
    /// void OnGUI()
    /// {
    ///     using (var header = new MaterialHeaderScope(text, ExpandBit, editor))
    ///     {
    ///         if (header.expanded)
    ///             EditorGUILayout.LabelField("Hello World !");
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public struct MaterialHeaderScope : IDisposable
    {
        /// <summary>Indicates whether the header is expanded or not. Is true if the header is expanded, false otherwise.</summary>
        public readonly bool expanded;
        bool spaceAtEnd;
#if !UNITY_2020_1_OR_NEWER
        int oldIndentLevel;
#endif

        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="title">GUI Content of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="isToggle">Set this to true to make the block include toggle at the front of its UI. Set to false to not include toggle.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        /// <param name="defaultExpandedState">The default state if the header is not present</param>
        /// <param name="documentationURL">[optional] Documentation page</param>
        public MaterialHeaderScope(GUIContent title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool isToggle = false, bool subHeader = false, uint defaultExpandedState = uint.MaxValue, string documentationURL = "")
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            bool beforeExpanded = materialEditor.IsAreaExpanded(bitExpanded, defaultExpandedState);
            bool isActive = materialEditor.IsAreaActive(bitExpanded, defaultExpandedState);
            bool beforeActive = isActive;

#if !UNITY_2020_1_OR_NEWER
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = subHeader ? 1 : 0; //fix for preset in 2019.3 (preset are one more indentation depth in material)
#endif

            this.spaceAtEnd = spaceAtEnd;
            if (!subHeader)
                CoreEditorUtils.DrawSplitter();
            GUILayout.BeginVertical();

            bool saveChangeState = GUI.changed;
            expanded = isToggle
                ? CoreEditorUtils.DrawToggleFoldout(title, beforeExpanded, ref isActive, null, null, null, documentationURL) 
                : subHeader
                    ? CoreEditorUtils.DrawSubHeaderFoldout(title, beforeExpanded, isBoxed: false)
                    : CoreEditorUtils.DrawHeaderFoldout(title, beforeExpanded, documentationURL: documentationURL);
            if (expanded ^ beforeExpanded)
            {
                materialEditor.SetIsAreaExpanded((uint)bitExpanded, expanded);
                saveChangeState = true;
            }
            if (isActive ^ beforeActive)
            {
                materialEditor.SetIsAreaActive((uint)bitExpanded, isActive);
                saveChangeState = true;
            }
            GUI.changed = saveChangeState;

            if (expanded)
                ++EditorGUI.indentLevel;
        }

        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="title">Title of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="isToggle">Set this to true to make the block include toggle at the front of its UI. Set to false to not include toggle.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        public MaterialHeaderScope(string title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool isToggle = false, bool subHeader = false)
            : this(EditorGUIUtility.TrTextContent(title, string.Empty), bitExpanded, materialEditor, spaceAtEnd, isToggle, subHeader)
        {
        }
        
        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="rect">Rect of the header.</param>
        /// <param name="title">GUI Content of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="isToggle">Set this to true to make the block include toggle at the front of its UI. Set to false to not include toggle.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        /// <param name="defaultExpandedState">The default state if the header is not present</param>
        /// <param name="documentationURL">[optional] Documentation page</param>
        public MaterialHeaderScope(Rect rect, GUIContent title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool isToggle = false, bool subHeader = false, uint defaultExpandedState = uint.MaxValue, string documentationURL = "")
        {
            if (title == null)
                throw new ArgumentNullException(nameof(title));

            bool beforeExpanded = materialEditor.IsAreaExpanded(bitExpanded, defaultExpandedState);
            bool isActive = materialEditor.IsAreaActive(bitExpanded, defaultExpandedState);
            bool beforeActive = isActive;

#if !UNITY_2020_1_OR_NEWER
            oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = subHeader ? 1 : 0; //fix for preset in 2019.3 (preset are one more indentation depth in material)
#endif

            this.spaceAtEnd = spaceAtEnd;
            if (!subHeader)
            {
                CoreEditorUtils.DrawSplitter(rect);
                rect.yMin += 1.0f;
            }
            GUILayout.BeginVertical();

            bool saveChangeState = GUI.changed;
            expanded = isToggle
                ? CoreEditorUtils.DrawToggleFoldout(rect, title, beforeExpanded, ref isActive, null, null, null, documentationURL) 
                : subHeader
                    ? CoreEditorUtils.DrawSubHeaderFoldout(title, beforeExpanded, isBoxed: false)
                    : CoreEditorUtils.DrawHeaderFoldout(rect, title, beforeExpanded, documentationURL: documentationURL);
            if (expanded ^ beforeExpanded)
            {
                materialEditor.SetIsAreaExpanded((uint)bitExpanded, expanded);
                saveChangeState = true;
            }
            if (isActive ^ beforeActive)
            {
                materialEditor.SetIsAreaActive((uint)bitExpanded, isActive);
                saveChangeState = true;
            }
            GUI.changed = saveChangeState;

            if (expanded)
                ++EditorGUI.indentLevel;
        }

        /// <summary>
        /// Creates a material header scope to display the foldout in the material UI.
        /// </summary>
        /// <param name="rect">Rect of the header.</param>
        /// <param name="title">Title of the header.</param>
        /// <param name="bitExpanded">Bit index which specifies the state of the header (whether it is open or collapsed) inside Editor Prefs.</param>
        /// <param name="materialEditor">The current material editor.</param>
        /// <param name="spaceAtEnd">Set this to true to make the block include space at the bottom of its UI. Set to false to not include any space.</param>
        /// <param name="isToggle">Set this to true to make the block include toggle at the front of its UI. Set to false to not include toggle.</param>
        /// <param name="subHeader">Set to true to make this into a sub-header. This affects the style of the header. Set to false to make this use the standard style.</param>
        public MaterialHeaderScope(Rect rect, string title, uint bitExpanded, MaterialEditor materialEditor, bool spaceAtEnd = true, bool isToggle = false, bool subHeader = false)
            : this(rect, EditorGUIUtility.TrTextContent(title, string.Empty), bitExpanded, materialEditor, spaceAtEnd, isToggle, subHeader)
        {
        }

        /// <summary>Disposes of the material scope header and cleans up any resources it used.</summary>
        void IDisposable.Dispose()
        {
            if (expanded)
            {
                if (spaceAtEnd && (Event.current.type == EventType.Repaint || Event.current.type == EventType.Layout))
                    EditorGUILayout.Space();
                --EditorGUI.indentLevel;
            }

#if !UNITY_2020_1_OR_NEWER
            EditorGUI.indentLevel = oldIndentLevel;
#endif
            GUILayout.EndVertical();
        }
    }
}

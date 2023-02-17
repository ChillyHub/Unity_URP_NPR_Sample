using Controller.PorbeController;
using Tool;
using UnityEditor;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PlaneReflectProbeController))]
    public class PlaneReflectProbeControllerGUI : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (((PlaneReflectProbeController)target).reflectionProbe == null)
            {
                EditorGUILayout.HelpBox("Game object need to add reflection probe component", MessageType.Warning);
            }
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(AvatarNormalSmoothTool))]
    public class AvatarNormalSmoothToolGUI : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (((AvatarNormalSmoothTool)target).mesh == null)
            {
                EditorGUILayout.HelpBox("Can't find skinned mesh renderer", MessageType.Warning);
            }
        }
    }
}
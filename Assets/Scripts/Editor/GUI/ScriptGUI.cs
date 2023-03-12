using UnityEngine;

using Controller;
using PipelineRenderer;
using Tool;

namespace UnityEditor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(PlaneReflectProbeController))]
    public class PlaneReflectProbeControllerGUI : Editor
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
    public class AvatarNormalSmoothToolGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            if (GUILayout.Button("Smooth Normal"))
            {
                ((AvatarNormalSmoothTool)target).rebuild = true;
                ((AvatarNormalSmoothTool)target).SmoothNormals();
            }

            if (((AvatarNormalSmoothTool)target).mesh == null)
            {
                EditorGUILayout.HelpBox("Can't find skinned mesh renderer", MessageType.Warning);
            }
        }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(SkyboxLUTRenderer))]
    public class SkyboxLUTRendererGUI : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Bake LUT"))
            {
                ((SkyboxLUTRenderer)target).Bake();
            }

            bool noShader = ((SkyboxLUTRenderer)target).computeShader == null;
            bool noMaterial = ((SkyboxLUTRenderer)target).material == null;
            string a = noShader ? "LUT render computer shader" : "";
            string b = noMaterial ? "skybox render material" : "";
            string c = noShader & noMaterial ? " and " : "";
            if (noShader || noMaterial)
            {
                EditorGUILayout.HelpBox($"Need to set {a}{c}{b}.", MessageType.Warning);
            }
        }
    }
}
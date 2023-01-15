using UnityEditor;

using Controller.PorbeController;

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

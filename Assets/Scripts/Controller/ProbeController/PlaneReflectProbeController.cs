using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Controller.PorbeController
{
    [ExecuteAlways]
    public class PlaneReflectProbeController : MonoBehaviour
    {
        public ReflectionProbe reflectionProbe;

        // Start is called before the first frame update
        void Start()
        {
            if (GetComponent<ReflectionProbe>() != null)
            {
                Debug.LogWarning("Reflection probe conponent can't be self component (PlaneReflectProbeController)");
            }
            else if (GetComponentInChildren<ReflectionProbe>() != null)
            {
                reflectionProbe = GetComponentInChildren<ReflectionProbe>();
            }
            else if (reflectionProbe == null)
            {
                Debug.LogWarning("Can't find reflection probe conponent (PlaneReflectProbeController)");
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (reflectionProbe != null && EditorWindow.focusedWindow != null)
            {
                if (EditorWindow.focusedWindow.titleContent.text == "Game")
                {
                    UpdateGame();
                }
                else if (EditorWindow.focusedWindow.titleContent.text == "Scene")
                {
                    UpdateScene();
                }
            }
        }

        private void OnValidate()
        {
            Start();
        }

        void UpdateScene()
        {
            Vector3 cameraPos = SceneView.lastActiveSceneView.camera.transform.position;

            reflectionProbe.transform.position =
                    cameraPos + 2.0f * Vector3.Project(transform.position - cameraPos, transform.up);
        }

        void UpdateGame()
        {
            Vector3 cameraPos = Camera.main.transform.position;

            reflectionProbe.transform.position =
                    cameraPos + 2.0f * Vector3.Project(transform.position - cameraPos, transform.up);
        }
    }
}

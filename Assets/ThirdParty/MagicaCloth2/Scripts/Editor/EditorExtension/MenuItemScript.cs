// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp
using UnityEditor;
using UnityEngine;

namespace MagicaCloth2
{
    public class MenuItemScript
    {
        //=========================================================================================
        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Cloth", priority = 200)]
        static void AddMagicaCloth()
        {
            var obj = AddObject("Magica Cloth", false, false);
            var comp = obj.AddComponent<MagicaCloth>();
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Sphere Collider", priority = 200)]
        static void AddSphereCollider()
        {
            var obj = AddObject("Magica Sphere Collider", false, true);
            var comp = obj.AddComponent<MagicaSphereCollider>();
            //comp.size = new Vector3(0.1f, 0.1f, 0.1f);
            comp.SetSize(new Vector3(0.1f, 0.1f, 0.1f));
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Capsule Collider", priority = 200)]
        static void AddCapsuleCollider()
        {
            var obj = AddObject("Magica Capsule Collider", false, true);
            var comp = obj.AddComponent<MagicaCapsuleCollider>();
            //comp.size = new Vector3(0.05f, 0.05f, 0.3f);
            comp.SetSize(new Vector3(0.05f, 0.05f, 0.3f));
            comp.direction = MagicaCapsuleCollider.Direction.Y;
            comp.radiusSeparation = false;
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Create Other/Magica Cloth2/Magica Plane Collider", priority = 200)]
        static void AddPlaneCollider()
        {
            var obj = AddObject("Magica Plane Collider", false, true);
            var comp = obj.AddComponent<MagicaPlaneCollider>();
            Selection.activeGameObject = obj;
        }

        //=========================================================================================
        /// <summary>
        /// ヒエラルキーにオブジェクトを１つ追加する
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        static GameObject AddObject(string objName, bool addParentName, bool autoScale = false)
        {
            var parent = Selection.activeGameObject;

            GameObject obj = new GameObject(addParentName && parent ? objName + " (" + parent.name + ")" : objName);
            if (parent)
            {
                obj.transform.parent = parent.transform;
            }
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            if (autoScale && parent)
            {
                var scl = parent.transform.lossyScale;
                obj.transform.localScale = new Vector3(1.0f / scl.x, 1.0f / scl.y, 1.0f / scl.z);
            }
            else
                obj.transform.localScale = Vector3.one;

            return obj;
        }
    }
}

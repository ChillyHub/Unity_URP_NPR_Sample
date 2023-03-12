using System;
using UnityEngine;

namespace UnityEditor
{
    public class AvatarMaterialsEditor : MonoBehaviour
    {
        private static MaterialPropertyBlock s_Block;

        private static int s_IsNightId = Shader.PropertyToID("_NightToggle");

        public bool Override = true;
    
        [Space(20)]
        public bool IsNight = false;

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            if (s_Block != null)
            {
                if (Override)
                {
                    s_Block.SetFloat(s_IsNightId, (float)Convert.ToDouble(IsNight));
            
                    GetComponentInChildren<Renderer>().SetPropertyBlock(s_Block);
                }
            }
            else
            {
                s_Block = new MaterialPropertyBlock();
            }
        }
    }
}
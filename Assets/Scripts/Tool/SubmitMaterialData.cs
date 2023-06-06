using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tool
{
    [ExecuteAlways]
    public class SubmitMaterialData : MonoBehaviour
    {
        public Material faceMaterial;
        public Material hairMaterial;

        public GameObject headRootBone;

        private static readonly int FrontDirectionId = Shader.PropertyToID("_FrontDirection");
        private static readonly int RightDirectionId = Shader.PropertyToID("_RightDirection");

        private void Update()
        {
            if (headRootBone != null)
            {
                Vector4 frontDir = -(Vector4)headRootBone.transform.forward;
                Vector4 rightDir = -(Vector4)headRootBone.transform.right;
                
                if (faceMaterial != null)
                {
                    faceMaterial.SetVector(FrontDirectionId, frontDir);
                    faceMaterial.SetVector(RightDirectionId, rightDir);
                }

                if (hairMaterial != null)
                {
                    hairMaterial.SetVector(FrontDirectionId, frontDir);
                    hairMaterial.SetVector(RightDirectionId, rightDir);
                }
            }
        }
    }
}
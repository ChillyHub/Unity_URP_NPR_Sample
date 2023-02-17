using System;
using System.Linq;
using UnityEngine;

namespace Tool
{
    [ExecuteInEditMode]
    public class AvatarNormalSmoothTool : MonoBehaviour
    {
        [NonSerialized]
        public Mesh mesh = null;
        
        private void Awake()
        {
            var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                mesh = skinnedMesh.sharedMesh;

                if (mesh.colors.Length == mesh.vertices.Length)
                {
                    return;
                }
                
                var smoothNormals = new Vector2[mesh.vertices.Length];
                var verticesGroup = skinnedMesh.sharedMesh.vertices
                    .Select((vertex, index) => (vertex, index)).GroupBy(tuple => tuple.vertex);

                foreach (var group in verticesGroup)
                {
                    Vector3 smoothNormal = Vector3.zero;
                    foreach (var (vertex, index) in group)
                    {
                        smoothNormal += mesh.normals[index];
                    }
                    
                    smoothNormal.Normalize();
                    foreach (var (vertex, index) in group)
                    {
                        smoothNormals[index] = PackNormalOctQuadEncode(smoothNormal);
                    }
                }

                mesh.uv2 = smoothNormals;
            }
        }
        
        private Vector2 PackNormalOctQuadEncode(Vector3 n)
        {
            float nDot1 = Mathf.Abs(n.x) + Mathf.Abs(n.y) + Mathf.Abs(n.z);
            n /= Mathf.Max(nDot1, 1e-6f);
            float tx = Mathf.Clamp01(-n.z);
            Vector2 t = new Vector2(tx, tx);
            Vector2 res = new Vector2(n.x, n.y);
            return res + (res is { x: >= 0.0f, y: >= 0.0f } ? t : -t);
        }
    }
}
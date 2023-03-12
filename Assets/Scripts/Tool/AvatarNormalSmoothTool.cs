using System;
using System.Linq;
using UnityEngine;

namespace Tool
{
    [ExecuteAlways]
    public class AvatarNormalSmoothTool : MonoBehaviour
    {
        [NonSerialized]
        public Mesh mesh = null;
        
        [NonSerialized]
        public bool rebuild = false;

        public void SmoothNormals()
        {
            var skinnedMesh = GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinnedMesh != null)
            {
                mesh = skinnedMesh.sharedMesh;
                
                if (!rebuild)
                {
                    return;
                }
                
                Debug.Log("Smoothing normals");

                var packNormals = new Vector2[mesh.vertices.Length];
                var smoothNormals = new Vector4[mesh.vertices.Length];
                var verticesGroup = mesh.vertices
                    .Select((vertex, index) => (vertex, index)).GroupBy(tuple => tuple.vertex);
                
                // Calculate smooth normals
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
                        smoothNormals[index] = (Vector4)smoothNormal;
                    }
                }

                // Turn smooth normals from Object space to Tangent space
                for (int index = 0; index < mesh.vertices.Length; index++)
                {
                    Vector3 normalOS = mesh.normals[index];
                    Vector4 tangentOS = mesh.tangents[index];
                    Vector4 bitangentOS = GetBitangentOS(normalOS, tangentOS, skinnedMesh.transform);
                    tangentOS.w = 0.0f;
                    
                    Matrix4x4 tbn = Matrix4x4.identity;
                    tbn.SetRow(0, tangentOS.normalized);
                    tbn.SetRow(1, bitangentOS);
                    tbn.SetRow(2, (Vector4)normalOS.normalized);
                    
                    Vector4 smoothNormalTS = tbn * smoothNormals[index];
                    packNormals[index] = PackNormalOctQuadEncode(smoothNormalTS.normalized);
                }

                mesh.uv4 = packNormals;
                rebuild = false;
                
                Debug.Log("Smooth normals completed");
            }
        }
        
        private void Awake()
        {
            SmoothNormals();
        }

        private float GetOddNegativeScale(Transform trans)
        {
            float scale = Vector3.Dot(trans.localScale, Vector3.one);
            return scale >= 0.0f ? 1.0f : -1.0f;
        }

        private Vector4 GetBitangentOS(Vector3 normalOS, Vector4 tangentOS, Transform trans)
        {
            Vector3 bitangnet = Vector3.Cross(normalOS.normalized, ((Vector3)tangentOS).normalized) 
                                * (tangentOS.w * GetOddNegativeScale(trans));
            
            bitangnet.Normalize();
            return new Vector4(bitangnet.x, bitangnet.y, bitangnet.z, 0.0f);
        }

        private Vector2 PackNormalOctQuadEncode(Vector4 n)
        {
            return PackNormalOctQuadEncode((Vector3)n);
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
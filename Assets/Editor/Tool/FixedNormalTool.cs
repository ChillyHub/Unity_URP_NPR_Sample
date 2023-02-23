using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    public class FixedNormalTool
    {
        [MenuItem("Tool/FixedNormalTool")]
        public static void FixedNormals()
        {
            MeshFilter[] meshFilters = Selection.activeGameObject.GetComponentsInChildren<MeshFilter>();
            foreach (var meshFilter in meshFilters)
            {
                Mesh mesh = meshFilter.sharedMesh;
                WriteNewNormal(mesh, meshFilter.transform);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers =
                Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                WriteNewNormal(mesh, skinnedMeshRenderer.transform);
            }
        }

        private static void WriteNewNormal(Mesh mesh, Transform trans)
        {
            var map = new Dictionary<Vector3, Vector3>();
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                if (!map.ContainsKey(mesh.vertices[i]))
                {
                    map.Add(mesh.vertices[i], mesh.normals[i]);
                }
                else
                {
                    map[mesh.vertices[i]] += mesh.normals[i];
                }
            }

            var newNormals = new Vector2[mesh.vertexCount];
            for (int i = 0; i < mesh.vertexCount; ++i)
            {
                Vector3 normal = map[mesh.vertices[i]].normalized;
                
                Vector3 normalOS = mesh.normals[i];
                Vector4 tangentOS = mesh.tangents[i];
                Vector4 bitangentOS = GetBitangentOS(normalOS, tangentOS, trans);
                tangentOS.w = 0.0f;
                    
                Matrix4x4 tbn = Matrix4x4.identity;
                tbn.SetRow(0, tangentOS.normalized);
                tbn.SetRow(1, bitangentOS);
                tbn.SetRow(2, (Vector4)normalOS.normalized);
                    
                Vector4 smoothNormalTS = tbn * normal;
                newNormals[i] = PackNormalOctQuadEncode(smoothNormalTS.normalized);
            }
        
            mesh.uv4 = newNormals;
        }
        
        private static float GetOddNegativeScale(Transform trans)
        {
            float scale = Vector3.Dot(trans.localScale, Vector3.one);
            return scale >= 0.0f ? 1.0f : -1.0f;
        }

        private static Vector4 GetBitangentOS(Vector3 normalOS, Vector4 tangentOS, Transform trans)
        {
            Vector3 bitangnet = Vector3.Cross(normalOS.normalized, ((Vector3)tangentOS).normalized) 
                                * (tangentOS.w * GetOddNegativeScale(trans));
            
            bitangnet.Normalize();
            return new Vector4(bitangnet.x, bitangnet.y, bitangnet.z, 0.0f);
        }

        private static Vector2 PackNormalOctQuadEncode(Vector4 n)
        {
            return PackNormalOctQuadEncode((Vector3)n);
        }
        
        private static Vector2 PackNormalOctQuadEncode(Vector3 n)
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

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
                WriteNewNormal(mesh);
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers =
                Selection.activeGameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var skinnedMeshRenderer in skinnedMeshRenderers)
            {
                Mesh mesh = skinnedMeshRenderer.sharedMesh;
                WriteNewNormal(mesh);
            }
        }

        private static void WriteNewNormal(Mesh mesh)
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
                newNormals[i] = PackNormalOctQuadEncode(normal);
            }
        
            mesh.uv2 = newNormals;
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

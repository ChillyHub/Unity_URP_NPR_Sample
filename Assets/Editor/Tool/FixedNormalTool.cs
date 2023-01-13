using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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

        var newNormals = new Color[mesh.vertexCount];
        for (int i = 0; i < mesh.vertexCount; ++i)
        {
            Vector3 normal = map[mesh.vertices[i]].normalized;
            newNormals[i] = new Color(normal.x, normal.y, normal.z);
        }
        
        mesh.colors = newNormals;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneratedMesh
{
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> uvs = new List<Vector2>();
    List<List<int>> submeshIndices = new List<List<int>>();

    public List<Vector3> Vertices { get { return vertices; } set { vertices = value; }}
    public List<Vector3> Normals { get { return normals; } set { normals = value; }}
    public List<Vector2> UVs { get { return uvs; } set { uvs = value; }}
    public List<List<int>> SubmeshIndices { get { return submeshIndices; } set { submeshIndices = value; }}

    public void AddTriangle(MeshTriangle _triangle)
    {
        int currentVerticeCount = vertices.Count;

        Vertices.AddRange(_triangle.Vertices);
        Normals.AddRange(_triangle.Normals);
        UVs.AddRange(_triangle.UVs);

        if(SubmeshIndices.Count < _triangle.SubmeshIndex + 1)
        {
            Debug.Log("Submeshindices " + SubmeshIndices.Count);
            for (int i = submeshIndices.Count; i < (_triangle.SubmeshIndex + 1); i++)
            {
                Debug.Log(i);
                SubmeshIndices.Add(new List<int>());
            }
            Debug.Log("After " + SubmeshIndices.Count);

        }

        for (int i = 0; i < 3; i++)
        {
            SubmeshIndices[_triangle.SubmeshIndex].Add(currentVerticeCount + i);
        }
    }
}

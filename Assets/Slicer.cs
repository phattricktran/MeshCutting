using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer
{
    public static bool currentlyCutting;
    public static Mesh originalMesh;

    public static void Cut(GameObject _originalGameObject, Vector3 _contactPoint, Vector3 _direction, Material _cutMaterial = null,
        bool fill = true, bool _addRigidBody = false)
    {
        if (currentlyCutting)
        {
            return;
        }

        currentlyCutting = true;

        Plane plane = new Plane(_originalGameObject.transform.InverseTransformDirection(-_direction),
            Vector3.zero);

        originalMesh = _originalGameObject.GetComponent<MeshFilter>().mesh;
        List<Vector3> addedVertices = new List<Vector3>();

        GeneratedMesh leftMesh = new GeneratedMesh();
        GeneratedMesh rightMesh = new GeneratedMesh();

        int[] submeshIndices;
        int triangleIndexA, triangleIndexB, triangleIndexC;

        Debug.Log(originalMesh.vertices.Length);
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            submeshIndices = originalMesh.GetTriangles(i);

            for (int j = 0; j < submeshIndices.Length; j+= 3)
            {
                triangleIndexA = submeshIndices[j];
                triangleIndexB = submeshIndices[j + 1];
                triangleIndexC = submeshIndices[j + 2];

                MeshTriangle currentTriangle = GetTriangle(triangleIndexA, triangleIndexB, triangleIndexC, i);

                bool triangleALeftSide = plane.GetSide(originalMesh.vertices[triangleIndexA]);
                bool triangleBLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexB]);
                bool triangleCLeftSide = plane.GetSide(originalMesh.vertices[triangleIndexC]);

                if(triangleALeftSide && triangleBLeftSide && triangleCLeftSide)
                {
                    leftMesh.AddTriangle(currentTriangle);
                } else if (!triangleALeftSide && !triangleBLeftSide && !triangleCLeftSide)
                {
                    rightMesh.AddTriangle(currentTriangle);
                }
            }
        }

        GenerateGameObject(leftMesh);
        GenerateGameObject(rightMesh);
    }

    public static MeshTriangle GetTriangle(int indexA, int indexB, int indexC, int submeshIndex)
    {
        Vector3[] vertices = new Vector3[] {
            originalMesh.vertices[indexA],
            originalMesh.vertices[indexB],
            originalMesh.vertices[indexC]
        };

        Vector3[] normals = new Vector3[] {
            originalMesh.normals[indexA],
            originalMesh.normals[indexB],
            originalMesh.normals[indexC]
        };

        Vector2[] uvs = new Vector2[] { };

        Debug.Log(vertices);
        return new MeshTriangle(vertices, normals, uvs, submeshIndex);
    }

    public static void GenerateGameObject(GeneratedMesh mesh)
    {
        GameObject newGameObject = new GameObject("Generated Slice");
        newGameObject.AddComponent<MeshFilter>();
        newGameObject.AddComponent<MeshRenderer>();

        newGameObject.GetComponent<MeshFilter>().mesh.vertices = mesh.Vertices.ToArray();
        newGameObject.GetComponent<MeshFilter>().mesh.normals = mesh.Normals.ToArray();

        for (int i = 0; i < mesh.SubmeshIndices.Count; i++)
        {
            newGameObject.GetComponent<MeshFilter>().mesh.SetTriangles(mesh.SubmeshIndices[i], i);
        }
    }
}

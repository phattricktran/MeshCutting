using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class with static methods to facilitate mesh slicing
/// Modified from https://youtu.be/1UsuZsaUUng
/// </summary>
public class Slicer
{
    public static bool currentlyCutting;
    public static Mesh originalMesh;
    public static GameObject originalGameObject;

    public static void Cut(GameObject _originalGameObject, Vector3 _contactPoint, Vector3 _direction, Material _cutMaterial = null,
        bool fill = true, bool _addRigidBody = false)
    {
        if (currentlyCutting)
        {
            return;
        }

        currentlyCutting = true;

        Plane plane = new Plane(_originalGameObject.transform.InverseTransformDirection(-_direction),
            _originalGameObject.transform.InverseTransformPoint(_contactPoint));

        originalGameObject = _originalGameObject;
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

                /* 
                 *  Three different cases:
                 *  - The triangle is either above the plane
                 *  - The triangle is below the plane
                 *  - The place intersects the triangle
                 */
                if(triangleALeftSide && triangleBLeftSide && triangleCLeftSide)
                {
                    leftMesh.AddTriangle(currentTriangle);
                } else if (!triangleALeftSide && !triangleBLeftSide && !triangleCLeftSide)
                {
                    rightMesh.AddTriangle(currentTriangle);
                }
                else 
                {
                    CutTriangle(plane, currentTriangle, triangleALeftSide, triangleBLeftSide, triangleCLeftSide, leftMesh, rightMesh, addedVertices);
                }
            }
        }

        FillCut(addedVertices, plane, leftMesh, rightMesh);

        GenerateGameObject(leftMesh);
        GenerateGameObject(rightMesh);
        Object.Destroy(_originalGameObject);
        currentlyCutting = false;
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

        Vector2[] uvs = new Vector2[]
        {
            originalMesh.uv[indexA],
            originalMesh.uv[indexB],
            originalMesh.uv[indexC]
        };

        return new MeshTriangle(vertices, normals, uvs, submeshIndex);
    }

    /// <summary>
    /// Takes a triangle, splits it into two, and places it in the correct GeneratedMesh
    /// </summary>
    private static void CutTriangle(Plane _plane, MeshTriangle _triangle, bool _triangleALeftSide, bool _triangleBLeftSide, bool _triangleCLeftSide,
        GeneratedMesh _leftSide, GeneratedMesh _rightSide, List<Vector3> _addedVertices)
    {
        // Keep a list of where the triangle vertices lie left of the plane or below
        List<bool> leftSide = new List<bool>();
        leftSide.Add(_triangleALeftSide);
        leftSide.Add(_triangleBLeftSide);
        leftSide.Add(_triangleCLeftSide);

        // We generate two fake triangles with vertex data from the intersecting triangle
        MeshTriangle leftMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], _triangle.SubmeshIndex);
        MeshTriangle rightMeshTriangle = new MeshTriangle(new Vector3[2], new Vector3[2], new Vector2[2], _triangle.SubmeshIndex);

        // Place the vertices in either the left and right mesh, depending on which side the vertex lies in relation to the plane
        SortVerticesFromIntersectedTriangle(_triangle, leftSide, leftMeshTriangle, rightMeshTriangle);

        // Using the fake triangle, we generate either one or two triangles per side
        MakeTriangles(_plane, _triangle, _leftSide, _addedVertices, leftMeshTriangle, rightMeshTriangle, true);
        MakeTriangles(_plane, _triangle, _rightSide, _addedVertices, rightMeshTriangle, leftMeshTriangle, false);
    }

    private static void SortVerticesFromIntersectedTriangle(MeshTriangle _triangle, List<bool> leftSide, MeshTriangle leftMeshTriangle, MeshTriangle rightMeshTriangle)
    {
        // These boolean values help us determine if either 1 or 2 verices of the intersecting triangle lie on a specific side
        bool oneVertexLeft = false;
        bool oneVertexRight = false;

        // We sort the vertices of the triangle depending on which side of the plane it was on
        for (int i = 0; i < 3; i++)
        {
            if (leftSide[i])
            {
                // First time we have a vertex on the left side, we assume that that's the only vertex on that side
                if (!oneVertexLeft)
                {
                    oneVertexLeft = true;

                    leftMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    leftMeshTriangle.Vertices[1] = leftMeshTriangle.Vertices[0];

                    leftMeshTriangle.UVs[0] = _triangle.UVs[i];
                    leftMeshTriangle.UVs[1] = leftMeshTriangle.UVs[0];

                    leftMeshTriangle.Normals[0] = _triangle.Normals[i];
                    leftMeshTriangle.Normals[1] = leftMeshTriangle.Normals[0];
                }
                // If we encounter another vertex on the left side, simply overright what we had before
                else
                {
                    leftMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    leftMeshTriangle.Normals[1] = _triangle.Normals[i];
                    leftMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
            else
            {
                if (!oneVertexRight)
                {
                    oneVertexRight = true;

                    rightMeshTriangle.Vertices[0] = _triangle.Vertices[i];
                    rightMeshTriangle.Vertices[1] = rightMeshTriangle.Vertices[0];

                    rightMeshTriangle.UVs[0] = _triangle.UVs[i];
                    rightMeshTriangle.UVs[1] = rightMeshTriangle.UVs[0];

                    rightMeshTriangle.Normals[0] = _triangle.Normals[i];
                    rightMeshTriangle.Normals[1] = rightMeshTriangle.Normals[0];
                }
                else
                {
                    rightMeshTriangle.Vertices[1] = _triangle.Vertices[i];
                    rightMeshTriangle.Normals[1] = _triangle.Normals[i];
                    rightMeshTriangle.UVs[1] = _triangle.UVs[i];
                }
            }
        }
    }

    private static void MakeTriangles(Plane _plane, MeshTriangle _triangle, GeneratedMesh _currentSide, List<Vector3> _addedVertices, MeshTriangle currentMeshTriangle, MeshTriangle oppositeMeshTriangle, bool addVertices)
    {
        float normalizedDistance;
        float distance;

        // Get the distance from the vertex to the intersecting plane, in the direction of a vertex that we know exists on the other side of the intersection
        // From our original triangle
        _plane.Raycast(new Ray(currentMeshTriangle.Vertices[0], (oppositeMeshTriangle.Vertices[0] - currentMeshTriangle.Vertices[0]).normalized), out distance);
        normalizedDistance = distance / (oppositeMeshTriangle.Vertices[0] - currentMeshTriangle.Vertices[0]).magnitude;

        Vector3 vertLeft = Vector3.Lerp(currentMeshTriangle.Vertices[0], oppositeMeshTriangle.Vertices[0], normalizedDistance);
        Vector3 normalLeft = Vector3.Lerp(currentMeshTriangle.Normals[0], oppositeMeshTriangle.Normals[0], normalizedDistance);
        Vector2 uvLeft = Vector2.Lerp(currentMeshTriangle.UVs[0], oppositeMeshTriangle.UVs[0], normalizedDistance);

        _plane.Raycast(new Ray(currentMeshTriangle.Vertices[1], (oppositeMeshTriangle.Vertices[1] - currentMeshTriangle.Vertices[1]).normalized), out distance);

        normalizedDistance = distance / (oppositeMeshTriangle.Vertices[1] - currentMeshTriangle.Vertices[1]).magnitude;
        Vector3 vertRight = Vector3.Lerp(currentMeshTriangle.Vertices[1], oppositeMeshTriangle.Vertices[1], normalizedDistance);

        // Since we call this method twice, prevent adding new vertices twice
        if (addVertices)
        {
            _addedVertices.Add(vertLeft);
            _addedVertices.Add(vertRight);
        }
        Vector3 normalRight = Vector3.Lerp(currentMeshTriangle.Normals[1], oppositeMeshTriangle.Normals[1], normalizedDistance);
        Vector3 uvRight = Vector2.Lerp(currentMeshTriangle.UVs[1], oppositeMeshTriangle.UVs[1], normalizedDistance);

        MeshTriangle currentTriangle;
        Vector3[] updatedVertices = new Vector3[] { currentMeshTriangle.Vertices[0], vertLeft, vertRight };
        Vector3[] updatedNormals = new Vector3[] { currentMeshTriangle.Normals[0], normalLeft, normalRight };
        Vector2[] updatedUVs = new Vector2[] { currentMeshTriangle.UVs[0], uvLeft, uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);

        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangle(currentTriangle);
            }
            _currentSide.AddTriangle(currentTriangle);
        }

        updatedVertices = new Vector3[] { currentMeshTriangle.Vertices[0], currentMeshTriangle.Vertices[1], vertRight };
        updatedNormals = new Vector3[] { currentMeshTriangle.Normals[0], currentMeshTriangle.Normals[1], normalRight };
        updatedUVs = new Vector2[] { currentMeshTriangle.UVs[0], currentMeshTriangle.UVs[1], uvRight };

        currentTriangle = new MeshTriangle(updatedVertices, updatedNormals, updatedUVs, _triangle.SubmeshIndex);

        if (updatedVertices[0] != updatedVertices[1] && updatedVertices[0] != updatedVertices[2])
        {
            if (Vector3.Dot(Vector3.Cross(updatedVertices[1] - updatedVertices[0], updatedVertices[2] - updatedVertices[0]), updatedNormals[0]) < 0)
            {
                FlipTriangle(currentTriangle);
            }
            _currentSide.AddTriangle(currentTriangle);
        }
    }

    private static void FillCut(List<Vector3> _addedVertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> polygon = new List<Vector3>();

        for (int i = 0; i < _addedVertices.Count; i++)
        {
            if(!vertices.Contains(_addedVertices[i]))
            {
                polygon.Clear();
                polygon.Add(_addedVertices[i]);
                polygon.Add(_addedVertices[i + 1]);

                vertices.Add(_addedVertices[i]);
                vertices.Add(_addedVertices[i + 1]);

                EvaluatePairs(_addedVertices, vertices, polygon);
                Fill(polygon, _plane, _leftMesh, _rightMesh);
            }
        }
    }


    private static void EvaluatePairs(List<Vector3> _addedVertices, List<Vector3> _vertices, List<Vector3> _polygon)
    {
        bool isDone = false;
        while (!isDone)
        {
            isDone = true;
            for (int i = 0; i < _addedVertices.Count; i+=2)
            {
                if(_addedVertices[i] == _polygon[_polygon.Count - 1] && !_vertices.Contains(_addedVertices[i + 1]))
                {
                    isDone = false;
                    _polygon.Add(_addedVertices[i + 1]);
                    _vertices.Add(_addedVertices[i + 1]);
                }
                else if (_addedVertices[i+1] == _polygon[_polygon.Count - 1] && !_vertices.Contains(_addedVertices[i]))
                {
                    isDone = false;
                    _polygon.Add(_addedVertices[i]);
                    _vertices.Add(_addedVertices[i]);
                }
            }
        }
    }

    private static void Fill(List<Vector3> _vertices, Plane _plane, GeneratedMesh _leftMesh, GeneratedMesh _rightMesh)
    {
        Vector3 centerPosition = Vector3.zero;

        for (int i = 0; i < _vertices.Count; i++)
        {
            centerPosition += _vertices[i];
        }
        centerPosition = centerPosition / _vertices.Count;

        Vector3 up = new Vector3()
        {
            x = _plane.normal.x,
            y = _plane.normal.y,
            z = _plane.normal.z
        };

        Vector3 left = Vector3.zero;
        Vector3 displacement = Vector3.zero;
        Vector2 uv1 = Vector2.zero;
        Vector2 uv2 = Vector2.zero;

        for (int i = 0; i < _vertices.Count; i++)
        {
            displacement = _vertices[i] - centerPosition;
            uv1 = new Vector2()
            {
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            displacement = _vertices[(i + 1) % _vertices.Count] - centerPosition;
            uv2 = new Vector2()
            {
                x = .5f + Vector3.Dot(displacement, left),
                y = .5f + Vector3.Dot(displacement, up)
            };

            Vector3[] vertices = new Vector3[] { _vertices[i], _vertices[(i + 1) % _vertices.Count], centerPosition };
            Vector3[] normals = new Vector3[] { -_plane.normal, -_plane.normal, -_plane.normal };
            Vector2[] uvs = new Vector2[] { uv1, uv2, new Vector2(0.5f, 0.5f) };

            MeshTriangle currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            // Make sure triangle is facing the right way
            if(Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]), normals[0]) < 0)
            {
                FlipTriangle(currentTriangle);
            }

            _leftMesh.AddTriangle(currentTriangle);

            normals = new Vector3[] { _plane.normal, _plane.normal, _plane.normal };
            currentTriangle = new MeshTriangle(vertices, normals, uvs, originalMesh.subMeshCount + 1);

            if (Vector3.Dot(Vector3.Cross(vertices[1] - vertices[0], vertices[2] - vertices[0]), normals[0]) < 0)
            {
                FlipTriangle(currentTriangle);
            }
            _rightMesh.AddTriangle(currentTriangle);
        }
    }

    // Generate a GameObject with the mesh
    public static void GenerateGameObject(GeneratedMesh mesh)
    {
        GameObject newGameObject = new GameObject("Generated Slice");
        newGameObject.transform.localScale = originalGameObject.transform.localScale;
        newGameObject.transform.position = originalGameObject.transform.position;

        newGameObject.AddComponent<MeshFilter>();
        newGameObject.AddComponent<MeshRenderer>();
        newGameObject.AddComponent<MeshCollider>();

        newGameObject.GetComponent<MeshFilter>().mesh.vertices = mesh.Vertices.ToArray();
        newGameObject.GetComponent<MeshFilter>().mesh.normals = mesh.Normals.ToArray();

        newGameObject.GetComponent<MeshFilter>().mesh.SetUVs(0, mesh.UVs);

        Material[] originalMaterials = originalGameObject.GetComponent<MeshRenderer>().materials;
        Material[] materials = new Material[mesh.SubmeshIndices.Count];

        for (int i = 0; i < mesh.SubmeshIndices.Count; i++)
        {
            Debug.Log(i);
            if (i < originalMaterials.Length)
            {
                materials[i] = originalMaterials[i];
            } else
            {
                materials[i] = originalMaterials[0];
            }
        }
        newGameObject.GetComponent<MeshRenderer>().materials = materials;

        int submeshCount = mesh.SubmeshIndices.Count;
        newGameObject.GetComponent<MeshFilter>().mesh.subMeshCount = submeshCount;
        for (int i = 0; i < mesh.SubmeshIndices.Count; i++)
        {
            newGameObject.GetComponent<MeshFilter>().mesh.SetTriangles(mesh.SubmeshIndices[i], i);
        }

        
        newGameObject.AddComponent<Rigidbody>();
        newGameObject.GetComponent<MeshCollider>().sharedMesh = newGameObject.GetComponent<MeshFilter>().mesh;
        newGameObject.GetComponent<MeshCollider>().convex = true;
    }

    private static void FlipTriangle(MeshTriangle triangle)
    {
        triangle.Vertices.Reverse();
        triangle.Normals.Reverse();
        triangle.UVs.Reverse();
    }
}

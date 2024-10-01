using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CylinderMeshGenerator : MonoBehaviour {
    public float Radius = 1.0f;
    public float Length = 2.0f;
    public int Subdivisions = 32;   // radial subdivisions (TODO: add lengthwise subdivs?)
    public float WrapDegrees = 360;  // 2*Mathf.PI

    private MeshFilter _meshFilter;

    public Mesh GenerateCylinderMesh(float radius, float length, float degrees, int subdivisions) {
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();

        float angleStep = Mathf.Deg2Rad * degrees / subdivisions;
        float halflength = 0.5f * length;

        for (int i = 0; i < subdivisions; ++i) {
            float angle = i * angleStep;
            float nextAngle = (i + 1) * angleStep;

            // Body
            Vector3 vertex1 = new Vector3(radius * Mathf.Cos(angle), -halflength, radius * Mathf.Sin(angle));
            Vector3 vertex2 = new Vector3(radius * Mathf.Cos(nextAngle), -halflength, radius * Mathf.Sin(nextAngle));
            Vector3 vertex3 = new Vector3(radius * Mathf.Cos(angle), halflength, radius * Mathf.Sin(angle));
            Vector3 vertex4 = new Vector3(radius * Mathf.Cos(nextAngle), halflength, radius * Mathf.Sin(nextAngle));

            vertices.Add(vertex1);
            vertices.Add(vertex2);
            vertices.Add(vertex3);
            vertices.Add(vertex4);

            // UVs
            uvs.Add(new Vector2((float)i / subdivisions, 0));
            uvs.Add(new Vector2((float)(i + 1) / subdivisions, 0));
            uvs.Add(new Vector2((float)i / subdivisions, 1));
            uvs.Add(new Vector2((float)(i + 1) / subdivisions, 1));

            // Triangles
            int baseIndex = i * 4;
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 3);
        }

        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        return mesh;
    }

    // Use this for initialization
    //void OnEnable() {
    void Start() {

        _meshFilter = GetComponent<MeshFilter>();
        _meshFilter.mesh = GenerateCylinderMesh(Radius, Length, WrapDegrees, Subdivisions);
    }

    private void OnValidate() {
        if (_meshFilter is null)
            return;
        _meshFilter.mesh = GenerateCylinderMesh(Radius, Length, WrapDegrees, Subdivisions);
    }
}
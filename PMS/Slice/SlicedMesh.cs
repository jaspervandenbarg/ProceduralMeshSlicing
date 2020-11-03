using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Jasperbarg;
using System.Linq;

namespace Jasperbarg 
{
    public class SlicedMesh
    {
        List<Vector3> vertexList = new List<Vector3>();
        public int VertexCount { get => vertexList.Count; }
        List<Vector3> normalList = new List<Vector3>();
        List<Vector2> uvList = new List<Vector2>();
        List<List<int>> triangleList = new List<List<int>>();
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public int[][] triangles;

        private List<Vector3> correctedVertices = new List<Vector3>();
        private List<Vector3> correctedNormals = new List<Vector3>();
        private List<Vector2> correctedUvs = new List<Vector2>();
        private List<List<int>> correctedTriangles = new List<List<int>>();

        public PolygonCreator polygon;
        public List<int> materialIndex = new List<int>();
        private int newSubmesh = -1;

        public SlicedMesh() {}

        public void AddTriangle(int submesh, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 n1, Vector3 n2, Vector3 n3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            int triangleListIndex = triangleList.Count - 1;
            if (triangleListIndex < submesh && submesh > newSubmesh)
            {
                triangleList.Add(new List<int>());
                newSubmesh = submesh;
                triangleListIndex += 1;
            }

            triangleList[triangleListIndex].Add(vertexList.Count);
            vertexList.Add(v1);
            triangleList[triangleListIndex].Add(vertexList.Count);
            vertexList.Add(v2);
            triangleList[triangleListIndex].Add(vertexList.Count);
            vertexList.Add(v3);
            normalList.Add(n1);
            normalList.Add(n2);
            normalList.Add(n3);
            uvList.Add(uv1);
            uvList.Add(uv2);
            uvList.Add(uv3);
        }
        public void AddPolygon(FinishedPolygon polygon)
        {
            vertexList.AddRange(polygon.vertices);
            normalList.AddRange(polygon.normals);
            uvList.AddRange(polygon.uv);
            triangleList.Add(new List<int>());
            triangleList[triangleList.Count - 1].AddRange(polygon.triangles);
        }

        private void CorrectData()
        {
            for (int submesh = 0; submesh < triangleList.Count; submesh++)
            {
                if (correctedTriangles.Count - 1 < submesh)
                    correctedTriangles.Add(new List<int>());

                for (int t = 0; t < triangleList[submesh].Count; t++)
                {
                    //check if data was correct
                    bool corrected = false;
                    //if corrected data is still empty
                    if (correctedVertices.Count == 0)
                    {
                        correctedTriangles[submesh].Add(correctedVertices.Count);
                        correctedVertices.Add(vertexList[triangleList[submesh][t]]);
                        correctedNormals.Add(normalList[triangleList[submesh][t]]);
                        correctedUvs.Add(uvList[triangleList[submesh][t]]);
                    }
                    //else check new list if vertex with normal matches
                    else
                    {
                        for (int i = 0; i < correctedVertices.Count; i++)
                        {
                            //if vertex and normal of that triangle are already in the list, only add triangle with index i
                            if (correctedVertices[i] == vertexList[triangleList[submesh][t]] && correctedNormals[i] == normalList[triangleList[submesh][t]])
                            {
                                correctedTriangles[submesh].Add(i);
                                corrected = true;
                                break;
                            }
                        }
                        //if not corrected add new data
                        if (!corrected)
                        {
                            correctedTriangles[submesh].Add(correctedVertices.Count);
                            correctedVertices.Add(vertexList[triangleList[submesh][t]]);
                            correctedNormals.Add(normalList[triangleList[submesh][t]]);
                            correctedUvs.Add(uvList[triangleList[submesh][t]]);
                        }
                    }
                }
            }
        }

        private void FillArray()
        {
            CorrectData();

            vertices = correctedVertices.ToArray();
            normals = correctedNormals.ToArray();
            uv = correctedUvs.ToArray();
            triangles = new int[correctedTriangles.Count][];
            for (int i = 0; i < correctedTriangles.Count; i++)
                triangles[i] = correctedTriangles[i].ToArray();

            vertexList.Clear();
            normalList.Clear();
            uvList.Clear();
            triangleList.Clear();
            correctedVertices.Clear();
            correctedNormals.Clear();
            correctedUvs.Clear();
            correctedTriangles.Clear();
        }
        private void FillArrayUncorrected()
        {
            vertices = vertexList.ToArray();
            normals = normalList.ToArray();
            uv = uvList.ToArray();
            triangles = new int[triangleList.Count][];
            for (int i = 0; i < triangleList.Count; i++)
                triangles[i] = triangleList[i].ToArray();
        }
        public void FillArray(bool corrected = true)
        {
            if (corrected) FillArray();
            else FillArrayUncorrected();
        }


        public void MakeGameObject(GameObject original, Material sliceMaterial, string name, Vector3 force)
        {
            GameObject slice;
            slice = new GameObject(original.name + name);
            slice.transform.position = original.transform.position;
            slice.transform.rotation = original.transform.rotation;
            slice.transform.localScale = original.transform.localScale;
            slice.layer = original.layer;

            //build new mesh from slicedmesh data and add it to the gameobject
            Mesh mesh = new Mesh();
            mesh.name = original.name;
            mesh.vertices = vertices;
            mesh.normals = mesh.normals;
            mesh.uv = uv;
            mesh.subMeshCount = triangles.Length;
            //Debug.Log("submeshes: " + triangles.Length);
            for (int t = 0; t < triangles.Length; t++)
                mesh.SetTriangles(triangles[t], t, true);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            slice.AddComponent<MeshFilter>().mesh = mesh;

            //add mesh renderer with original component
            if (sliceMaterial == null) sliceMaterial = original.GetComponent<MeshRenderer>().material;
            MeshRenderer renderer = slice.AddComponent<MeshRenderer>();
            Material[] newMats = new Material[mesh.subMeshCount];
            Material[] originalMats = original.GetComponent<MeshRenderer>().materials;
            for (int i = 0; i < newMats.Length; i++)
            {
                if (i == newMats.Length - 1 && newMats.Length > 1)
                    newMats[i] = sliceMaterial;
                else
                    newMats[i] = originalMats[i];
            }
            renderer.materials = newMats;

            //add collider
            slice.AddComponent<MeshCollider>().convex = true;

            //add rigidbody
            Rigidbody rigid = slice.AddComponent<Rigidbody>();
            if (force != Vector3.zero) rigid.AddForce(force, ForceMode.Impulse);

            //debug vertices
            Debug.Log(slice.name + " has " + vertices.Length + " vertices");
        }
    }
}


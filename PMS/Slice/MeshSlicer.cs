using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using Jasperbarg;


public class MeshSlicer : MonoBehaviour
{
    public float rayLenght = 20, castAngle = 45, lines = 10;
    public LayerMask slicingLayer;
    public Material sliceMaterial;
    [Range(-5, 5)]
    public float force = 0;
    public bool concave = false, correctData = true, drawRays = false;

    private bool setEdge = false;
    private Vector3 edgeVertex = Vector3.zero;
    private Vector2 edgeUV = Vector2.zero;
    private Plane edgePlane = new Plane();

    void Update()
    {
        //draw ray and maybe other gizmos
        if (drawRays)
            DrawGizmos();

        //left mouse click
        if (Input.GetMouseButtonDown(0))
            Slice();
    }

    public void Slice()
    {
        //raycast targets forward, might change to multiple casts
        Ray ray = new Ray(transform.position, transform.forward);
        List<RaycastHit> _hits = new List<RaycastHit>();
        //cast a X amount of rays over a Y angle
        float halfAngle = castAngle;
        halfAngle /= 2;

        float minAngle = -halfAngle;
        float increment = (castAngle / (lines - 1));
        for (float i = minAngle; i <= halfAngle; i += increment)
        {
            Quaternion rayDirection = Quaternion.AngleAxis(i, transform.up);
            Ray _ray = new Ray(transform.position, (rayDirection * transform.forward).normalized);
            _hits.AddRange(Physics.RaycastAll(_ray, rayLenght, slicingLayer).ToList());
        }
        //take each first element of the grouped gameobjects, thus distincting them based on object instead of hit
        RaycastHit[] hits = _hits.GroupBy(g => g.transform.gameObject).Select(f => f.First()).ToArray();

        //for all objects that are hit
        for (int h = 0; h < hits.Length; h++)
        {
            Mesh hitMesh = hits[h].transform.GetComponent<MeshFilter>().mesh;
            List<SlicedMesh> slices = new List<SlicedMesh>();

            SlicedMesh unslicedMesh = new SlicedMesh()
            {
                uv = hitMesh.uv,
                vertices = hitMesh.vertices,
                normals = hitMesh.normals,
                triangles = new int[hitMesh.subMeshCount][],
            };
            for (int i = 0; i < hitMesh.subMeshCount; i++)
                unslicedMesh.triangles[i] = hitMesh.GetTriangles(i);

            //plane with normal up so the plane is parralel to the slice
            Plane plane = new Plane(hits[h].transform.InverseTransformDirection(transform.up), hits[h].transform.InverseTransformPoint(hits[h].point));

            slices.AddRange(GenerateMesh(unslicedMesh, plane, true, hits[h].transform));

            for (int s = 0; s < slices.Count; s++)
            {
                slices[s].MakeGameObject(hits[h].transform.gameObject, sliceMaterial, (s + 1).ToString(), transform.forward * force);
            }
            slices.Clear();
            Destroy(hits[h].transform.gameObject);
        }
    }

    private List<SlicedMesh> GenerateMesh(SlicedMesh original, Plane plane, bool positiveSide, Transform objectTransform)
    {
        SlicedMesh slicePositive = new SlicedMesh();
        SlicedMesh sliceNegative = new SlicedMesh();
        //polygon we use to fill in the sliced surface
        PolygonCreator polygonPositive = new PolygonCreator(objectTransform, plane.normal * -1);
        PolygonCreator polygonNegative = new PolygonCreator(objectTransform, plane.normal);
        bool matPositiveAdded = false, matNegativeAdded = false;

        //we loop over all submeshes
        for (int submesh = 0; submesh < original.triangles.Length; submesh++)
        {
            int[] originalTriangles = original.triangles[submesh];
            setEdge = false;

            //increase t by 3 because a triangle consist out of 3 vertices;
            for (int t = 0; t < originalTriangles.Length; t += 3)
            {
                //which triangle we need
                int t1 = t, t2 = t + 1, t3 = t + 2;

                //Check if vertice is on positive side of the plane
                bool sideA = plane.GetSide(original.vertices[originalTriangles[t1]]) == positiveSide;
                bool sideB = plane.GetSide(original.vertices[originalTriangles[t2]]) == positiveSide;
                bool sideC = plane.GetSide(original.vertices[originalTriangles[t3]]) == positiveSide;

                //how many vertices are on the positive side of the plane
                int sideCount = (sideA ? 1 : 0) +
                                (sideB ? 1 : 0) +
                                (sideC ? 1 : 0);

                //if none of the vertices is located on the positive side
                if (sideCount == 0)
                {
                    //add entire triangle to negative side
                    sliceNegative.AddTriangle(submesh, original.vertices[originalTriangles[t1]], original.vertices[originalTriangles[t2]], original.vertices[originalTriangles[t3]],
                                      original.normals[originalTriangles[t1]], original.normals[originalTriangles[t2]], original.normals[originalTriangles[t3]],
                                      original.uv[originalTriangles[t1]], original.uv[originalTriangles[t2]], original.uv[originalTriangles[t3]]);
                    if (!matNegativeAdded)
                    {
                        matNegativeAdded = true;
                        sliceNegative.materialIndex.Add(submesh);
                    }

                    continue;
                }
                //if all the vertices are located on the positive side
                else if (sideCount == 3)
                {
                    //add entire triangle to positive side
                    slicePositive.AddTriangle(submesh, original.vertices[originalTriangles[t1]], original.vertices[originalTriangles[t2]], original.vertices[originalTriangles[t3]],
                                      original.normals[originalTriangles[t1]], original.normals[originalTriangles[t2]], original.normals[originalTriangles[t3]],
                                      original.uv[originalTriangles[t1]], original.uv[originalTriangles[t2]], original.uv[originalTriangles[t3]]);
                    if (!matPositiveAdded)
                    {
                        matPositiveAdded = true;
                        slicePositive.materialIndex.Add(submesh);
                    }

                    continue;
                }

                //else a triangle is cut and submesh material must be added to both sides
                if (!matNegativeAdded)
                {
                    matNegativeAdded = true;
                    sliceNegative.materialIndex.Add(submesh);
                }
                if (!matPositiveAdded)
                {
                    matPositiveAdded = true;
                    slicePositive.materialIndex.Add(submesh);
                }


                //determines which vertex in the triangle is solely located on one side of the plane
                int singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;
                int indexB = t + ((singleIndex + 1) % 3), indexC = t + ((singleIndex + 2) % 3);
                singleIndex += t;

                //calculate which vertices/normals/uv should be used to calculate intersection points
                Vector3 singleVertex = original.vertices[originalTriangles[singleIndex]],
                        vertexB = original.vertices[originalTriangles[indexB]],                 //right vertex
                        vertexC = original.vertices[originalTriangles[indexC]];                 //left vertex
                Vector3 singleNormal = original.normals[originalTriangles[singleIndex]],
                        normalB = original.normals[originalTriangles[indexB]],
                        normalC = original.normals[originalTriangles[indexC]];
                Vector2 singleUv = original.uv[originalTriangles[singleIndex]],
                        uvB = original.uv[originalTriangles[indexB]],
                        uvC = original.uv[originalTriangles[indexC]];

                //calculate new vertices/normals/uv where edge intersects plane
                float lerpB, lerpC;
                Vector3 newVertexB = PointOnPlane(plane, singleVertex, vertexB, out lerpB),     //new right vertex
                        newVertexC = PointOnPlane(plane, singleVertex, vertexC, out lerpC);     //new left vertex
                Vector3 newNormalB = Vector3.Lerp(singleNormal, normalB, lerpB),                //lerp to get the point between the old vertices where the new vertex is located
                        newNormalC = Vector3.Lerp(singleNormal, normalC, lerpC);
                Vector2 newUvB = Vector2.Lerp(singleUv, uvB, lerpB),
                        newUvC = Vector2.Lerp(singleUv, uvC, lerpC);

                if (!concave)
                {
                    //add and edge to "fill" the mesh
                    AddSliceTriangle(submesh, slicePositive, newVertexB, newVertexC,
                                         plane.normal * -1,
                                         newUvB, newUvC);
                    AddSliceTriangle(submesh, sliceNegative, newVertexB, newVertexC,
                                         plane.normal,
                                         newUvB, newUvC);
                }

                if (sideCount == 1)
                {
                    //positive data
                    slicePositive.AddTriangle(submesh, singleVertex, newVertexB, newVertexC, singleNormal, newNormalB, newNormalC, singleUv, newUvB, newUvC);
                    //negative data
                    sliceNegative.AddTriangle(submesh, newVertexB, vertexB, vertexC, newNormalB, normalB, normalC, newUvB, uvB, uvC);
                    sliceNegative.AddTriangle(submesh, newVertexB, vertexC, newVertexC, newNormalB, normalC, newNormalC, newUvB, uvC, newUvC);

                    if (concave)
                    {
                        //positive
                        Edge edgePositive = new Edge(newVertexB, newVertexC, plane.normal * -1, newUvB, newUvC);
                        polygonPositive.AddEdge(edgePositive);
                        //negative
                        Edge edgeNegative = new Edge(newVertexC, newVertexB, plane.normal, newUvC, newUvB);
                        polygonNegative.AddEdge(edgeNegative);
                    }
                    continue;
                }
                else if (sideCount == 2)
                {
                    //positive data
                    slicePositive.AddTriangle(submesh, newVertexB, vertexB, vertexC, newNormalB, normalB, normalC, newUvB, uvB, uvC);
                    slicePositive.AddTriangle(submesh, newVertexB, vertexC, newVertexC, newNormalB, normalC, newNormalC, newUvB, uvC, newUvC);
                    //negative data
                    sliceNegative.AddTriangle(submesh, singleVertex, newVertexB, newVertexC, singleNormal, newNormalB, newNormalC, singleUv, newUvB, newUvC);
                    if (concave)
                    {
                        //positive
                        Edge edgePositive = new Edge(newVertexC, newVertexB, plane.normal * -1, newUvC, newUvB);
                        polygonPositive.AddEdge(edgePositive);
                        //negative
                        Edge edgeNegative = new Edge(newVertexB, newVertexC, plane.normal, newUvB, newUvC);
                        polygonNegative.AddEdge(edgeNegative);
                    }
                    continue;
                }
            }
        }

        if (concave)
        {
            //build polygons
            polygonPositive.ConnectEdges();
            polygonNegative.ConnectEdges();

            //build meshdata for polygons
            FinishedPolygon polygonPositiveFinished = Triangulation.Triangulate(polygonPositive, slicePositive.VertexCount);
            FinishedPolygon polygonNegativeFinished = Triangulation.Triangulate(polygonNegative, sliceNegative.VertexCount);

            //add meshdata to slices
            slicePositive.AddPolygon(polygonPositiveFinished);
            sliceNegative.AddPolygon(polygonNegativeFinished);
        }

        slicePositive.FillArray(correctData);
        sliceNegative.FillArray(correctData);

        return new List<SlicedMesh>() { slicePositive, sliceNegative };
    }

    private void AddSliceTriangle(int subMesh, SlicedMesh slice, Vector3 v1, Vector3 v2, Vector3 normal, Vector2 uv1, Vector2 uv2)
    {
        if (!setEdge)
        {
            setEdge = true;
            edgeVertex = v1;
            edgeUV = uv1;
        }
        else
        {
            edgePlane.Set3Points(edgeVertex, v1, v2);

            slice.AddTriangle(subMesh,
                                edgeVertex,
                                edgePlane.GetSide(edgeVertex + normal) ? v1 : v2,
                                edgePlane.GetSide(edgeVertex + normal) ? v2 : v1,
                                normal,
                                normal,
                                normal,
                                edgeUV,
                                uv1,
                                uv2);
        }
    }

    private Edge AddEdge(bool positiveSide, Vector3 v1, Vector3 v2, Vector3 normal, Vector2 uv1, Vector2 uv2)
    {
        return new Edge(positiveSide ? v1 : v2,
                        positiveSide ? v2 : v1,
                        normal,
                        positiveSide ? uv1 : uv2,
                        positiveSide ? uv2 : uv1);
    }

    //calculate where the edge of the triangle is cut
    //v1 is the vertex that is on the other side of the plane as the other two vertices
    private Vector3 PointOnPlane(Plane _plane, Vector3 v1, Vector3 v2, out float lerp)
    {
        Vector3 direction = (v2 - v1);
        Ray ray = new Ray(v1, direction.normalized);
        float distance;
        _plane.Raycast(ray, out distance);
        Vector3 v3 = v1 + (direction.normalized * distance);
        lerp = distance / direction.magnitude;
        return v3;
    }

    private void DrawGizmos()
    {
        float halfAngle = castAngle;
        halfAngle /= 2;

        float minAngle = -halfAngle;
        float increment = (castAngle / (lines - 1));
        for (float i = minAngle; i <= halfAngle; i += increment)
        {
            Quaternion rayDirection = Quaternion.AngleAxis(i, transform.up);
            Debug.DrawRay(transform.position, (rayDirection * transform.forward).normalized * rayLenght, Color.red);
        }
    }
}

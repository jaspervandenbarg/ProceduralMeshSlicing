using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

namespace Jasperbarg
{
    public class Earclipping
    {
        /// <summary>
        /// List of all vertices (holes connected) in clockwise order
        /// </summary>
        List<Vector2> vertices2D;
        List<Vector3> vertices3D;
        List<Vector2> uvs;
        List<int> convexVertices = new List<int>();
        List<int> reflexVertices = new List<int>();
        List<int> earVertices = new List<int>();
        List<int> verticesToEmpty = new List<int>();
        bool positiveSide;
        List<int> triangles = new List<int>();
        private int triangleOffset;
        //check which lists are needed and which lists should be checked

        public List<Vector3> Vertices { get => vertices3D; }
        public List<Vector2> UV { get => uvs; }
        public List<int> Triangles { get => triangles; }

        public Earclipping(EarClippingPolygon polygon, int triangleOffset = 0)
        {
            this.triangleOffset = triangleOffset;

            if (polygon.Holes.Count > 0)
            {
                vertices3D = polygon.Outline.Vertices3D;
                vertices2D = polygon.Outline.Vertices2D;
                uvs = polygon.Outline.UV;
                ConnectHoles(polygon.Holes);
            }
            else
            {
                vertices3D = polygon.Outline.Vertices3D;
                vertices2D = polygon.Outline.Vertices2D;
                uvs = polygon.Outline.UV;
            }

            FillInitialTriangulationData();
        }

        //fills the reflex, convex and ear list
        private void FillInitialTriangulationData()
        {
            int verticesCount = vertices2D.Count;

            //verticesToEmpty
            for (int i = 0; i < verticesCount; i++)
            {
                verticesToEmpty.Add(i);
            }

            //check which ears are convex and reflex
            for (int i = 0; i < verticesCount; i++)
            {
                if (IsConvexVertex(vertices2D[(verticesCount + i - 1) % verticesCount], vertices2D[i], vertices2D[(i + 1) % verticesCount]))
                {
                    convexVertices.Add(i);
                }
                else
                {
                    reflexVertices.Add(i);
                }
            }
            //after that we can check for all the convex vertices if any reflex vertices are located inside the triangle of vL,vM,vR
            for (int i = 0; i < convexVertices.Count; i++)
            {
                int vM = convexVertices[i];
                int vL = IndexBerfore(vM);
                int vR = IndexAfter(vM);

                if (IsEar(vertices2D[vL], vertices2D[vM], vertices2D[vR]))
                {
                    earVertices.Add(convexVertices[i]);
                }
                else continue;
            }

            //Debug.Log("Vertices: " + vertices2D.Count + " Reflex: " + reflexVertices.Count + " Convex: " + convexVertices.Count + " Ears: " + earVertices.Count);
        }

        public void ClipEars()
        {
            while (verticesToEmpty.Count > 3)
            {

                if (earVertices.Count == 0)
                {
                    Debug.LogError("0 EarVertices found but there are still " + verticesToEmpty.Count + " VerticesToEmpty");
                }

                //Vertices that make up the triangle
                int vM = earVertices.First();
                int vL = IndexBerfore(vM);
                int vR = IndexAfter(vM);

                //the new triangle
                //left handed order of the triangle
                triangles.AddRange(new List<int>() { vM + triangleOffset, vL + triangleOffset, vR + triangleOffset });
                //right handed order of the triangle
                //triangles.AddRange(new List<int>() { vL, vM, vR });
                earVertices.Remove(vM);
                convexVertices.Remove(vM);
                verticesToEmpty.Remove(vM);

                //check for new reflex/convex/ear vertices
                int vT;
                //check vL
                vT = IndexBerfore(vL);
                UpdateVertices(vT, vL, vR);
                //check vR  
                vT = IndexAfter(vR);
                UpdateVertices(vL, vR, vT);

            }
            //if vertices to empty == 3 thats the last triangle
            if (verticesToEmpty.Count == 3)
            {
                int vL = verticesToEmpty[0]; ;
                int vM = verticesToEmpty[1];
                int vR = verticesToEmpty[2];
                //the new triangle
                //left handed order of the triangle
                triangles.AddRange(new List<int>() { vM + triangleOffset, vL + triangleOffset, vR + triangleOffset });
                //right handed order of the triangle
                //triangles.AddRange(new List<int>() { vL, vM, vR });
                earVertices.Remove(vM);
                verticesToEmpty.Clear();
            }
            //Debug.Log("remaining vertices: " + verticesToEmpty.Count);

        }

        /// <summary>
        /// If true vertex is convex. If false vertex is concave/reflex
        /// </summary>
        /// <param name="vL">Vertex located before the vertex to be checked</param>
        /// <param name="vM">Vertex to be checked</param>
        /// <param name="vR">Vertex located behind the vertex to be checked</param>
        /// <returns></returns>
        private bool IsConvexVertex(Vector2 vL, Vector2 vM, Vector2 vR)
        {
            if ((vM - vL).normalized == (vR - vM).normalized) return true;
            Plane plane = new Plane(vL, vM, Vector3.back);
            return plane.GetSide(vR);
        }

        //computational efficient way to check if reflex vertex is inside triangle
        /// <summary>
        /// Is convec vertex also an ear?
        /// </summary>
        /// <param name="vL">vertex infront of the vertex to be checked</param>
        /// <param name="vM">vertex to be checked</param>
        /// <param name="vR">vertex after the vertex to be checked</param>
        /// <returns>True if none of the reflex vertices is located inside the triangle</returns>
        private bool IsEar(Vector2 vL, Vector2 vM, Vector2 vR)
        {
            for (int i = 0; i < reflexVertices.Count; i++)
            {
                Vector2 P = vertices2D[reflexVertices[i]];

                if (P == vL || P == vR) continue;

                double s1 = vR.y - vM.y;
                double s2 = vR.x - vM.x;
                double s3 = vL.y - vM.y;
                double s4 = P.y - vM.y;

                double w1 = (vM.x * s1 + s4 * s2 - P.x * s1) / (s3 * s2 - (vL.x - vM.x) * s1);
                double w2 = (s4 - w1 * s3) / s1;
                if (w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1) return false;
                else continue;
            }
            return true;
        }

        private void ConnectHoles(List<PolygonMetaData> holes)
        {
            List<PolygonMetaData> holesToEmpty = new List<PolygonMetaData>(holes);

            while (holesToEmpty.Count > 0)
            {
                Vector2 maxPoint2D = Vector2.zero;
                Vector3 maxPoint3D = Vector3.zero;
                float maxX = holesToEmpty[0].Vertices2D[0].x;
                int vertexIndex = 0, holeIndex = 0;
                for (int i = 0; i < holesToEmpty.Count; i++)
                {
                    for (int j = 0; j < holesToEmpty[i].Vertices2D.Count; j++)
                    {
                        float tempX = holesToEmpty[i].Vertices2D[j].x;
                        if (tempX >= maxX)
                        {
                            maxPoint3D = holesToEmpty[i].Vertices3D[j];
                            maxPoint2D = holesToEmpty[i].Vertices2D[j];
                            maxX = tempX;
                            vertexIndex = j;
                            holeIndex = i;
                        }
                    }
                }

                //find what vertex to connect
                //make a list of linesegments where atleast 1 xposition is bigger than maxX
                int connectingVertexIndex = FindVerticesToConnect(maxPoint2D);
                List<Vector3> newVertices3D = new List<Vector3>();
                List<Vector2> newVertices2D = new List<Vector2>();
                List<Vector2> connectionUV = new List<Vector2>();
                //rebuild list starting from vertex index ending with maxXPoint and connecting vertex
                int holeSize = holesToEmpty[holeIndex].Vertices2D.Count;
                for (int i = 0; i < holeSize; i++)
                {
                    newVertices3D.Add(holesToEmpty[holeIndex].Vertices3D[(vertexIndex + i) % holeSize]);
                    newVertices2D.Add(holesToEmpty[holeIndex].Vertices2D[(vertexIndex + i) % holeSize]);
                    connectionUV.Add(holesToEmpty[holeIndex].UV[(vertexIndex + i) % holeSize]);
                }
                newVertices3D.Add(maxPoint3D);  //3D
                newVertices3D.Add(vertices3D[connectingVertexIndex]);   //3D
                newVertices2D.Add(maxPoint2D);  //2D
                newVertices2D.Add(vertices2D[connectingVertexIndex]);   //2D
                connectionUV.Add(holesToEmpty[holeIndex].UV[vertexIndex]);  //UV
                connectionUV.Add(uvs[connectingVertexIndex]);   //UV

                //add this new list to the vertices list;
                vertices3D.InsertRange(connectingVertexIndex + 1, newVertices3D);   //3D
                vertices2D.InsertRange(connectingVertexIndex + 1, newVertices2D);   //2D
                uvs.InsertRange(connectingVertexIndex + 1, connectionUV);   //UV

                holesToEmpty.RemoveAt(holeIndex);
            }

        }
        /// <summary>
        /// Checks for all suitable vertices in the outer polygon which one is suited to be connected to
        /// </summary>
        /// <param name="hV">The vertex of the hole that needs to be connected to the outer polygon</param>
        /// <returns>Returns an index of the vertices List for which vertex to connect hV to</returns>
        private int FindVerticesToConnect(Vector2 hV)
        {
            List<Edge2D> possibleEdges = new List<Edge2D>();
            Vector2 possiblePoint = Vector2.zero;
            int possibleIndex = 0;
            int verticesCount = vertices2D.Count;

            bool connectingEdgeFound = false;

            //fill possible edges
            for (int i = 0; i < verticesCount; i++)
            {
                //check which edges have atleast one x value thats greater than hv.x
                Vector2 v1 = vertices2D[i];
                Vector2 v2 = vertices2D[(i + 1) % verticesCount];
                if (v1.x > hV.x || v2.x > hV.x)
                {
                    //add new line segment to possible edges
                    possibleEdges.Add(new Edge2D(v1, v2, i, (i + 1) % verticesCount));
                }
            }

            //get edge connection to start with
            for (int i = 0; i < possibleEdges.Count; i++)
            {
                //find the initial edge to connect to
                Vector2 sP = possibleEdges[i].StartPosition;
                Vector2 eP = possibleEdges[i].EndPosition;
                //check if we intersect and edge, if so connect to the vertex with the highest x position;
                if ((sP.y < hV.y && eP.y >= hV.y) || (sP.y >= hV.y && eP.y < hV.y))
                {
                    bool sPb = Mathf.Max(sP.x, eP.x) == sP.x;
                    possiblePoint = sPb ? sP : eP;
                    possibleIndex = sPb ? possibleEdges[i].StartIndex : possibleEdges[i].EndIndex;
                    possibleEdges.RemoveAt(i);
                    break;
                }
                else continue;
            }

            //while connecting edge is still not found
            while (!connectingEdgeFound)
            {
                //create line from hole point to new connection point
                Edge2D cE = new Edge2D(hV, possiblePoint);

                //check for each edge if intersecting
                for (int i = 0; i < possibleEdges.Count; i++)
                {
                    Vector2 sP = possibleEdges[i].StartPosition;
                    Vector2 eP = possibleEdges[i].EndPosition;

                    //Debug.Log("edge: " + cE.EndPosition);

                    if (sP != cE.EndPosition && eP != cE.EndPosition && cE.Intersect(possibleEdges[i]))
                    {
                        //if intersecting edgefound is false, set new vertex, set new index, break loop
                        connectingEdgeFound = false;
                        bool sPb = Mathf.Max(sP.x, eP.x) == sP.x;
                        possiblePoint = sPb ? sP : eP;
                        possibleIndex = sPb ? possibleEdges[i].StartIndex : possibleEdges[i].EndIndex;
                        possibleEdges.RemoveAt(i);
                        break;
                    }
                    else
                    {
                        //if loop is not broken connecting edge is found.
                        connectingEdgeFound = true;
                        continue;
                    }
                }
            }
            return possibleIndex;
        }


        /// <summary>
        /// Gets the index of the vertex located before vM
        /// </summary>
        /// <param name="vM">Vertex index</param>
        /// <returns>Index of vertex located before vM in the list</returns>
        private int IndexBerfore(int vM)
        {
            int index = verticesToEmpty.IndexOf(vM);
            int count = verticesToEmpty.Count;
            return verticesToEmpty[(count + index - 1) % count];
        }
        /// <summary>
        /// Gets the index of the vertex located after vM
        /// </summary>
        /// <param name="vM">Vertex index</param>
        /// <returns>Index of vertex located after vM in the list</returns>
        private int IndexAfter(int vM)
        {
            int index = verticesToEmpty.IndexOf(vM);
            int count = verticesToEmpty.Count;
            return verticesToEmpty[(count + index + 1) % count];
        }

        /// <summary>
        /// Update adjacent vertices of the ear that has been clipped
        /// </summary>
        /// <param name="iL">Vertex index infront of the vertex to be updated</param>
        /// <param name="iM">Vertex index to be updated</param>
        /// <param name="iR">Vertex index after the the vertex to be updated</param>
        private void UpdateVertices(int iL, int iM, int iR)
        {
            Vector2 vL = vertices2D[iL];
            Vector2 vM = vertices2D[iM];
            Vector2 vR = vertices2D[iR];

            if (reflexVertices.Contains(iM))
            {
                //check if is now convex
                if (IsConvexVertex(vL, vM, vR))
                {
                    convexVertices.Add(iM);
                    reflexVertices.Remove(iM);
                    //check if is ear
                    if (IsEar(vL, vM, vR))
                        earVertices.Insert(0, iM);
                }
            }
            else
            {
                bool wasEar = earVertices.Contains(iM);
                //check if is ear
                if (IsEar(vL, vM, vR))
                {
                    if (!wasEar)
                        earVertices.Insert(0, iM);
                }
                else
                {
                    if (wasEar)
                        earVertices.Remove(iM);
                }
            }
        }
    }
}


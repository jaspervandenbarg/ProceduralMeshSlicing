using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Jasperbarg
{
    public class PolygonCreator
    {

        private Vector3 polygonNormal;
        private Transform parentTransform;
        public Vector3 Normal { get => polygonNormal; }

        //unconnected/unordered edges from the cutting plane
        private List<Edge> rawEdges = new List<Edge>();
        //list with a list of edges that are connected/ordered
        private List<List<Edge>> orderedEdges = new List<List<Edge>>();
        //list of polygons
        private List<PolygonMetaData> individualPolygons = new List<PolygonMetaData>();
        //list of polygons that are outlines
        private List<PolygonMetaData> outlines = new List<PolygonMetaData>();
        //list of polygons that are holes
        private List<PolygonMetaData> holes = new List<PolygonMetaData>();

        public List<PolygonMetaData> Outlines { get => outlines; }
        public List<PolygonMetaData> Holes { get => holes; }

        /// <summary>
        /// A new Polygon with normal and transform
        /// </summary>
        /// <param name="normal">The normal of the cutting plane</param>
        /// <param name="transform">Transform of the original mesh, this is needed to project 3D points to 2D</param>
        public PolygonCreator(Transform transform, Vector3 normal)
        {
            this.polygonNormal = normal;
            this.parentTransform = transform;
        }

        public void AddEdge(Edge edge)
        {
            rawEdges.Add(edge);
        }

        // Take the raw edge list and connect the edges.
        // If a full circle/polygon is formed but there are still edges left -> create a new edge list
        public void ConnectEdges()
        {
            int currentList = 0;
            List<Edge> listToEmpy = new List<Edge>(rawEdges);

            while (listToEmpy.Count > 0)
            {
                if (orderedEdges.Count - 1 < currentList)
                    orderedEdges.Add(new List<Edge>());

                if (orderedEdges[currentList].Count == 0)
                {
                    orderedEdges[currentList].Add(listToEmpy[0]);
                    listToEmpy.RemoveAt(0);
                }

                Edge nextEdge = orderedEdges[currentList][orderedEdges[currentList].Count - 1];

                while (true)
                {
                    bool nextEgdeFound = false;
                    for (int i = 0; i < listToEmpy.Count; i++)
                    {
                        if (nextEdge.EndPosition == listToEmpy[i].StartPositon)
                        {
                            nextEgdeFound = true;
                            orderedEdges[currentList].Add(listToEmpy[i]);
                            listToEmpy.RemoveAt(i);
                            nextEdge = orderedEdges[currentList].Last();
                        }
                    }
                    if (!nextEgdeFound)
                    {
                        currentList++;
                        break;
                    }
                }
            }
            MakePolygons();
            FilterHolesAndOutlines();
        }

        private void MakePolygons()
        {
            for (int i = 0; i < orderedEdges.Count; i++)
            {
                List<Vector3> tempVertices3D = new List<Vector3>();
                List<Vector2> tempVertices2D = new List<Vector2>();
                List<Vector2> tempUV = new List<Vector2>();

                for (int j = 0; j < orderedEdges[i].Count; j++)
                {
                    tempVertices3D.Add(orderedEdges[i][j].StartPositon);
                    tempVertices2D.Add(Project3DTo2D(orderedEdges[i][j].StartPositon, polygonNormal));
                    tempUV.Add(orderedEdges[i][j].StartUV);
                }
                individualPolygons.Add(new PolygonMetaData(tempVertices3D, tempVertices2D, tempUV, polygonNormal, i));
            }
            orderedEdges.Clear();
        }

        private void FilterHolesAndOutlines()
        {
            outlines = new List<PolygonMetaData>(individualPolygons);
            individualPolygons.Clear();

            bool noNewChanges = false, noNewOutlines = false, noNewHoles = false;

            while (!noNewChanges)
            {
                noNewChanges = true;
                //if there are new outlines, check if they are a hole in another outline
                if (!noNewOutlines && outlines.Count > 1)
                {
                    noNewOutlines = true;
                    List<PolygonMetaData> tempOutlines = new List<PolygonMetaData>();
                    List<PolygonMetaData> tempHoles = new List<PolygonMetaData>();
                    //for every outline check if there is a temp outline inside it
                    for (int i = 0; i < outlines.Count; i++)
                    {
                        bool isInside = false;
                        for (int j = 0; j < outlines.Count; j++)
                        {
                            if(i != j && outlines[i].Parent == outlines[j].Parent)
                            {
                                if (outlines[j].IsPointInPolygon(outlines[i].Vertices2D[UnityEngine.Random.Range(0, outlines[i].Vertices2D.Count - 1)]))
                                {
                                    noNewChanges = false;
                                    noNewHoles = false;
                                    PolygonMetaData tempPolygon = new PolygonMetaData(outlines[i], outlines[j].ID);
                                    tempHoles.Add(tempPolygon);
                                    isInside = true;
                                    break;
                                }
                                else continue;
                            }
                        }
                        if (!isInside)
                        {
                            tempOutlines.Add(outlines[i]);
                        }
                    }
                    outlines = new List<PolygonMetaData>(tempOutlines);
                    holes.AddRange(tempHoles);
                    tempHoles.Clear();
                    tempOutlines.Clear();
                }

                if (!noNewHoles && holes.Count > 1)
                {
                    noNewHoles = true;
                    List<PolygonMetaData> tempOutlines = new List<PolygonMetaData>();
                    List<PolygonMetaData> tempHoles = new List<PolygonMetaData>();
                    //for every outline check if there is a temp outline inside it
                    for (int i = 0; i < holes.Count; i++)
                    {
                        bool isInside = false;
                        for (int j = 0; j < holes.Count; j++)
                        {
                            if (i != j && holes[i].Parent == holes[j].Parent)
                            {
                                if (holes[j].IsPointInPolygon(holes[i].Vertices2D[UnityEngine.Random.Range(0, holes[i].Vertices2D.Count - 1)]))
                                {
                                    noNewChanges = false;
                                    noNewOutlines = false;
                                    PolygonMetaData tempPolygon = new PolygonMetaData(holes[i], holes[i].Parent);
                                    tempOutlines.Add(tempPolygon);
                                    isInside = true;
                                    break;
                                }
                                else continue;
                            }
                        }
                        if (!isInside)
                        {
                            tempHoles.Add(holes[i]);
                        }
                    }
                    holes = new List<PolygonMetaData>(tempHoles);
                    outlines.AddRange(tempOutlines);
                    tempHoles.Clear();
                    tempOutlines.Clear();
                }
            }
            individualPolygons.Clear();
        }

        private Vector2 Project3DTo2D(Vector3 point, Vector3 normal)
        {
            Vector3 u;
            if (Mathf.Abs(Vector3.Dot(parentTransform.forward, normal)) < 0.2f)
                u = Vector3.ProjectOnPlane(parentTransform.right, normal);
            else
                u = Vector3.ProjectOnPlane(parentTransform.forward, normal);

            Vector3 v = Vector3.Cross(u, normal).normalized;
            return new Vector2(Vector3.Dot(point, u), Vector3.Dot(point, v));
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

namespace Jasperbarg
{
    public class PolygonMetaData
    {
        private List<Vector3> vertices3D = new List<Vector3>();
        private List<Vector2> vertices2D = new List<Vector2>();
        private Vector3 normal;
        private List<Vector2> uv = new List<Vector2>();
        private int parent, id;

        public int Parent { get => parent; }
        public int ID { get => id; }
        public List<Vector3> Vertices3D { get => vertices3D; }
        public List<Vector2> Vertices2D { get => vertices2D; }
        public List<Vector2> UV { get => uv; }
        public Vector3 Normal { get => normal; }

        public PolygonMetaData(List<Vector3> vertices3D, List<Vector2> vertices2D, List<Vector2> uv, Vector3 normal, int id, int parentIndex = -1)
        {
            this.vertices3D = vertices3D;
            this.vertices2D = vertices2D;
            this.uv = uv;
            this.normal = normal;
            this.parent = parentIndex;
            this.id = id;
        }
        public PolygonMetaData(PolygonMetaData polygon, int parentIndex = -1)
        {
            this.vertices3D = polygon.Vertices3D;
            this.vertices2D = polygon.Vertices2D;
            this.uv = polygon.UV;
            this.normal = polygon.Normal;
            this.parent = parentIndex;
            this.id = polygon.ID;
        }

        public void SetParent(int parent)
        {
            this.parent = parent;
        }

        public bool IsPointInPolygon(Vector2 point)
        {
            bool inside = false;

            int polygonLenght = vertices2D.Count;
            for (int i = 0; i < vertices2D.Count; i++)
            {
                int j = (i + 1) % polygonLenght;
                //if point.y lies between both y values of the edge it may intersect
                if ((vertices2D[i].y < point.y && vertices2D[j].y >= point.y) || (vertices2D[i].y >= point.y && vertices2D[j].y < point.y))
                {
                    //get the direction vector to calculate the x position of the edge at the height of the point
                    Vector2 direction = vertices2D[j] - vertices2D[i];
                    Vector2 newPoint = point - vertices2D[i];
                    float factor = (newPoint.y / direction.y);
                    if (newPoint.x < direction.x * factor && direction.y != 0)
                        inside = !inside;
                }
            }
            return inside;
        }
    }

    public class FinishedPolygon
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector2> uv = new List<Vector2>();
        public List<int> triangles = new List<int>();
    }

    public class EarClippingPolygon
    {
        PolygonMetaData outerPolygon;
        List<PolygonMetaData> holes = new List<PolygonMetaData>();
        Vector3 normal;

        public PolygonMetaData Outline { get => outerPolygon; }
        public List<PolygonMetaData> Holes { get => holes; }
        public Vector3 Normal { get => normal; }

        public EarClippingPolygon(PolygonMetaData outerPolygon)
        {
            this.outerPolygon = outerPolygon;
        }

        public void AddHole(PolygonMetaData hole)
        {
            this.holes.Add(hole);
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jasperbarg
{
    public class Edge
    {
        private Vector3 startPosition, endPosition, normal;
        private Vector2 startUV, endUV;

        public Vector3 StartPositon { get => startPosition; }
        public Vector3 EndPosition { get => endPosition; }
        /// <summary>
        /// Normal of the slicing plane according the the side the edge is located on
        /// </summary>
        public Vector3 Normal { get => normal; }
        public Vector2 StartUV { get => startUV; }
        public Vector2 EndUV { get => endUV; }

        public Edge(Vector3 startPosition, Vector3 endPosition, Vector3 normal, Vector2 startUV, Vector2 endUV)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.normal = normal;
            this.startUV = startUV;
            this.endUV = endUV;
        }
    }

    public class Edge2D
    {
        private Vector2 startPosition, endPosition;
        private int startIndex, endIndex;
        public Vector2 StartPosition { get => startPosition; }
        public Vector2 EndPosition { get => endPosition; }
        public int StartIndex { get => startIndex; }
        public int EndIndex { get => endIndex; }

        public Edge2D(Vector2 startPosition, Vector2 endPosition, int startIndex, int endIndex)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
            this.startIndex = startIndex;
            this.endIndex = endIndex;
        }
        public Edge2D(Vector2 startPosition, Vector2 endPosition)
        {
            this.startPosition = startPosition;
            this.endPosition = endPosition;
        }

        /// <summary>
        /// Check if Line is intersecting with other
        /// </summary>
        /// <param name="other">Line to be checked against line</param>
        /// <returns>True or False</returns>
        public bool Intersect(Edge2D other)
        {
            //intersection = Vector2.zero;

            Vector2 p1 = startPosition;
            Vector2 p2 = endPosition;
            Vector2 p3 = other.startPosition;
            Vector2 p4 = other.endPosition;

            var d = (p2.x - p1.x) * (p4.y - p3.y) - (p2.y - p1.y) * (p4.x - p3.x);

            if (d == 0.0f)
            {
                return false;
            }

            var u = ((p3.x - p1.x) * (p4.y - p3.y) - (p3.y - p1.y) * (p4.x - p3.x)) / d;
            var v = ((p3.x - p1.x) * (p2.y - p1.y) - (p3.y - p1.y) * (p2.x - p1.x)) / d;

            if (u < 0.0f || u > 1.0f || v < 0.0f || v > 1.0f)
            {
                return false;
            }

            //intersection.x = p1.x + u * (p2.x - p1.x);
            //intersection.y = p1.y + u * (p2.y - p1.y);

            return true;
        }
    }
}


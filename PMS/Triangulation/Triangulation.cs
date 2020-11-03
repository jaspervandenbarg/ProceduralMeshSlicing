using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Jasperbarg
{
    /// <summary>
    /// Class to process the polygon to make a mesh out of it
    /// </summary>
    public static class Triangulation
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="polygon">Polygon to triangulate</param>
        /// <param name="verticesCount">lenght of the vertexList of the mesh that has been sliced - 1</param>
        public static FinishedPolygon Triangulate(PolygonCreator polygon, int verticesCount)
        {
            FinishedPolygon finishedPolygon = new FinishedPolygon();

            //polygon creator has outlines and holes;

            //for each outline we need a seperate polygon to triangulate
            for(int i = 0; i < polygon.Outlines.Count; i++)
            {
                EarClippingPolygon earClipPoly = new EarClippingPolygon(new PolygonMetaData(polygon.Outlines[i]));
                for(int j = 0; j < polygon.Holes.Count; j++)
                {
                    if(polygon.Holes[j].Parent == polygon.Outlines[i].ID)
                    {
                        earClipPoly.AddHole(new PolygonMetaData(polygon.Holes[j]));
                    }
                }
                //outline is combined with hole(s) if any and is ready to be earclipped
                //add triangle offset to make up for vertices that are already inside the mesh
                int triangleOffsetIndex = verticesCount + finishedPolygon.vertices.Count;

                Earclipping earClipper = new Earclipping(earClipPoly, triangleOffsetIndex);
                earClipper.ClipEars();

                finishedPolygon.triangles.AddRange(earClipper.Triangles);
                finishedPolygon.uv.AddRange(earClipper.UV);
                finishedPolygon.vertices.AddRange(earClipper.Vertices);
                for(int n = 0; n < earClipper.Vertices.Count; n++)
                {
                    finishedPolygon.normals.Add(polygon.Normal);
                }
            }
            return finishedPolygon;
        }
    }
}


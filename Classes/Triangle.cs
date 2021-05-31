using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Triangle : Polygon
{
    public bool EdgeWasRemoved = false;

    public Triangle(List<Edge> edges)
    {
        this.Edges = edges;
    }

    public static bool IsTrianglesSame(Triangle firstTriangle, Triangle secondTriangle)
    {
        var firstTriangleEdges = firstTriangle.Edges.ToArray();

        var isTrianglesSame = secondTriangle.Edges.Any(secondTriangleEdge =>
            Edge.IsEdgesSame(firstTriangleEdges[0], secondTriangleEdge)
        ) && secondTriangle.Edges.Any(secondTriangleEdge =>
            Edge.IsEdgesSame(firstTriangleEdges[1], secondTriangleEdge)
        ) && secondTriangle.Edges.Any(secondTriangleEdge =>
            Edge.IsEdgesSame(firstTriangleEdges[2], secondTriangleEdge));

        return isTrianglesSame;
    }
}
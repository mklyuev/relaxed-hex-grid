using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Edge
{
    public List<Vertex> Vertices = new List<Vertex>(2);
    
    public List<Triangle> Triangles = new List<Triangle>();

    public bool IsLastRing = false;

    public Edge(Vertex firstVertex, Vertex secondVertex)
    {
        Vertices.Add(firstVertex);
        Vertices.Add(secondVertex);
    }

    public static bool IsEdgesSame(Edge firstEdge, Edge secondEdge)
    {
        var firstEdgeVertices = firstEdge.Vertices.ToArray();
        var secondEdgeVertices = secondEdge.Vertices.ToArray();

        var isEdgesSame = (firstEdgeVertices[0].Position == secondEdgeVertices[0].Position || firstEdgeVertices[0].Position == secondEdgeVertices[1].Position)
            && (firstEdgeVertices[1].Position == secondEdgeVertices[0].Position || firstEdgeVertices[1].Position == secondEdgeVertices[1].Position);

        return isEdgesSame;
    }
}
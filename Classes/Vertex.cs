using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Vertex
{
    public Vector3 Position;
    
    public List<Edge> Edges = new List<Edge>();

    public int RingIndex;

    public Vertex(Vector3 position, int ringIndex)
    {
        this.Position = position;
        this.RingIndex = ringIndex;
    }
}
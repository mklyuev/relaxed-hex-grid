using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Quad : Polygon
{
    public Quad(List<Edge> edges)
    {
        this.Edges = edges;
    }
}
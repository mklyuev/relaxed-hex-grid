using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using SystemRandom = System.Random;
using Random = UnityEngine.Random;

public class HexGrid : MonoBehaviour
{
    public int ringCount = 5;
    
    public float outerRadius = 1f;
    private float innerRadius;

    private List<Vertex> vertices = new List<Vertex>();
    private List<Edge> edges = new List<Edge>();
    private List<Triangle> triangles = new List<Triangle>();
    private List<Quad> quads = new List<Quad>();

    private void Awake()
    {
        innerRadius = outerRadius * 0.866025404f;
        
        StartCoroutine(GenerateRelaxedHexGrid());
    }
    
    private IEnumerator GenerateRelaxedHexGrid()
    {
        yield return StartCoroutine(GenerateVertices());
        yield return StartCoroutine(GenerateTriangles());
        yield return StartCoroutine(MakeQuadsFromTriangles());
        yield return StartCoroutine(SplitPolygons());
        yield return StartCoroutine(RelaxVertices());
    }
    
    private IEnumerator GenerateVertices()
    {
        vertices.Add(new Vertex(new Vector3(), -1));

        yield return StartCoroutine(LagOfAnimation(0.01f));

        for (var ringIndex = 0; ringIndex < ringCount; ringIndex++)
        {
            var newInnerRadius = innerRadius + innerRadius * ringIndex;   
            var newOuterRadius = outerRadius + outerRadius * ringIndex;

            // hex has 6 edges, each edge 2 vertex plus vertices between them depends on ring index
            yield return StartCoroutine(AddRingEdge(new Vector3(0, newOuterRadius), new Vector3(newInnerRadius, 0.5f * newOuterRadius), ringIndex));
            yield return StartCoroutine(AddRingEdge(new Vector3(newInnerRadius, 0.5f * newOuterRadius), new Vector3(newInnerRadius, -0.5f * newOuterRadius), ringIndex));
            yield return StartCoroutine(AddRingEdge(new Vector3(newInnerRadius, -0.5f * newOuterRadius), new Vector3(0,  -newOuterRadius), ringIndex));
            yield return StartCoroutine(AddRingEdge(new Vector3(0,  -newOuterRadius), new Vector3(-newInnerRadius, -0.5f * newOuterRadius), ringIndex));
            yield return StartCoroutine(AddRingEdge(new Vector3(-newInnerRadius, -0.5f * newOuterRadius), new Vector3(-newInnerRadius, 0.5f * newOuterRadius), ringIndex));
            yield return StartCoroutine(AddRingEdge(new Vector3(-newInnerRadius, 0.5f * newOuterRadius), new Vector3(0, newOuterRadius), ringIndex));
        }
    }
    
    private IEnumerator AddRingEdge(Vector3 startPoint, Vector3 endPoint, int ringIndex)
    {
        // first vertex of hex edge
        var vertex = FindVertex(startPoint);
        if (vertex == null)
        {
            vertices.Add(new Vertex(startPoint, ringIndex));
        }
        
        yield return StartCoroutine(LagOfAnimation(0.01f));

        // adding vertices to the edge 
        var step = 1f / (ringIndex + 1);
        for (var i = 0; i < ringIndex; i++)
        {
            var nextPoint = step + step * i;

            vertices.Add(new Vertex(Vector3.Lerp(
                startPoint,
                endPoint,
                nextPoint),ringIndex ));
            yield return StartCoroutine(LagOfAnimation(0.01f));
        }
        
        // second vertex of hex edge
        vertex = FindVertex(endPoint);
        if (vertex == null)
        {
            vertices.Add(new Vertex(endPoint, ringIndex));
        }
    }

    private IEnumerator GenerateTriangles()
    {
        for (var ringIndex = 0; ringIndex < ringCount - 1; ringIndex++)
        {
            var ringVertices = vertices.Where(vertex => vertex.RingIndex == ringIndex).ToList();

            foreach (var firstPointOfTriangle in ringVertices)
            {
                for (var degreesIndex = 1; degreesIndex <= 6; degreesIndex++)
                {
                    var secondPointOfTriangle = GetVertexByDegreesFromStartPoint(60 * degreesIndex - 30, firstPointOfTriangle);
                    var thirdPointOfTriangle = GetVertexByDegreesFromStartPoint(60 * (degreesIndex + 1) - 30, firstPointOfTriangle);
                    
                    var firstEdge = FindEdge(new Edge(firstPointOfTriangle, secondPointOfTriangle));
                    if (firstEdge == null)
                    {
                        firstEdge = new Edge(firstPointOfTriangle, secondPointOfTriangle);
                        firstPointOfTriangle.Edges.Add(firstEdge);
                        secondPointOfTriangle.Edges.Add(firstEdge);
                        edges.Add(firstEdge);
                        yield return StartCoroutine(LagOfAnimation(0.01f));
                    }

                    var secondEdge = FindEdge(new Edge(secondPointOfTriangle, thirdPointOfTriangle));
                    if (secondEdge == null)
                    {
                        secondEdge = new Edge(secondPointOfTriangle, thirdPointOfTriangle);
                        // second edge is always 'top' of triangle and could be outside of hex
                        if (secondPointOfTriangle.RingIndex == ringCount - 1)
                        {
                            secondEdge.IsLastRing = true;
                        }
                        secondPointOfTriangle.Edges.Add(secondEdge);
                        thirdPointOfTriangle.Edges.Add(secondEdge);
                        edges.Add(secondEdge);
                        yield return StartCoroutine(LagOfAnimation(0.01f));
                    }
                    
                    var thirdEdge = FindEdge(new Edge(thirdPointOfTriangle, firstPointOfTriangle));
                    if (thirdEdge == null)
                    {
                        thirdEdge = new Edge(thirdPointOfTriangle, firstPointOfTriangle);
                        thirdPointOfTriangle.Edges.Add(thirdEdge);
                        firstPointOfTriangle.Edges.Add(thirdEdge);
                        edges.Add(thirdEdge);
                        yield return StartCoroutine(LagOfAnimation(0.01f));
                    }
                    
                    var triangle = new Triangle(new List<Edge>(3) {firstEdge, secondEdge, thirdEdge});
                    if (IsTriangleAlreadyExist(triangle))
                    {
                        continue;
                    }
                    firstEdge.Triangles.Add(triangle);
                    secondEdge.Triangles.Add(triangle);
                    thirdEdge.Triangles.Add(triangle);
                        
                    triangles.Add(triangle);
                }
            }
        }
    }
    
    private Vertex GetVertexByDegreesFromStartPoint(float degrees, Vertex startPoint)
    {
        var radians = degrees * Mathf.Deg2Rad;
        var x = Mathf.Cos(radians);
        var y = Mathf.Sin(radians);
        
        return vertices.Where(vertex => vertex.Position == startPoint.Position + new Vector3(x, y, 0) * outerRadius).ToArray()[0];
    }
    
    private IEnumerator MakeQuadsFromTriangles()
    {
        var rnd = new SystemRandom();
        foreach (var triangle in triangles.OrderBy(item => rnd.Next()).Where(x => !x.EdgeWasRemoved))
        {
            var triedIndices = new List<int>();
            var isEdgeRemoved = false;

            while (!isEdgeRemoved && triedIndices.Count < 3)
            {
                var randomIndex = Random.Range(0, 3);
                if (triedIndices.Contains(randomIndex))
                {
                    continue;
                }
                triedIndices.Add(randomIndex);

                var edgeToRemove = triangle.Edges.ToArray()[randomIndex];
                if (edgeToRemove.IsLastRing)
                {
                    continue;
                }

                var edgeTriangles = edgeToRemove.Triangles;
                var isEdgeCouldBeRemoved = edgeTriangles.All(edgeTriangle => !edgeTriangle.EdgeWasRemoved);

                if (isEdgeCouldBeRemoved)
                {
                    edges.Remove(edgeToRemove);
                    foreach (var vertex in edgeToRemove.Vertices)
                    {
                        vertex.Edges.Remove(edgeToRemove);
                    }
                    
                    yield return StartCoroutine(LagOfAnimation(0.01f));
                    isEdgeRemoved = true;

                    // creating quad
                    var quadEdges = new List<Edge>();
                    foreach (var edgeTriangle in edgeTriangles)
                    {
                        edgeTriangle.Edges.Remove(edgeToRemove);   
                        edgeTriangle.EdgeWasRemoved = true;
                        quadEdges.Add(edgeTriangle.Edges.ToArray()[0]);
                        quadEdges.Add(edgeTriangle.Edges.ToArray()[1]);
                    }
                    
                    quads.Add(new Quad(quadEdges));
                }
            }
        }
        
        yield return null;
    }

    private IEnumerator SplitPolygons()
    {
        yield return StartCoroutine(SplitToQuads(quads));
        yield return StartCoroutine(SplitToQuads(triangles.Where(x => x.Edges.Count == 3)));
    }
    private IEnumerator SplitToQuads(IEnumerable<Polygon> polygons)
    {
        foreach (var polygon in polygons)
        {
            var edgeCenters = new List<Vertex>();
            var quadCenter = new Vector3();
            foreach (var edge in polygon.Edges)
            {
                var centerOfEdge = (edge.Vertices.ToArray()[0].Position + edge.Vertices.ToArray()[1].Position) / 2;
                var isLastRing = edge.Vertices.ToArray()[0].RingIndex == ringCount - 1 &&
                                 edge.Vertices.ToArray()[1].RingIndex == ringCount - 1;
                
                var vertex = FindVertex(centerOfEdge);
                if (vertex == null)
                {
                    edges.Remove(edge);
                    
                    vertex = new Vertex(centerOfEdge, (isLastRing) ? ringCount - 1 : 0);
                    vertices.Add(vertex);
                    
                    var newEdge = new Edge(edge.Vertices.ToArray()[0], vertex);
                    edges.Add(newEdge);
                    edge.Vertices.ToArray()[0].Edges.Remove(edge);
                    edge.Vertices.ToArray()[0].Edges.Add(newEdge);
                    vertex.Edges.Add(newEdge);
                    
                    newEdge = new Edge(edge.Vertices.ToArray()[1], vertex);
                    edges.Add(newEdge);
                    edge.Vertices.ToArray()[1].Edges.Remove(edge);
                    edge.Vertices.ToArray()[1].Edges.Add(newEdge);
                    vertex.Edges.Add(newEdge);
                }

                edgeCenters.Add(vertex);
                quadCenter += vertex.Position;
                yield return StartCoroutine(LagOfAnimation(0.01f));
            }

            var vertexOfQuadCenter = new Vertex(quadCenter / polygon.Edges.Count, 0);
            vertices.Add(vertexOfQuadCenter);

            foreach (var edgeCenter in edgeCenters)
            {
                var edgeToCenter = new Edge(edgeCenter, vertexOfQuadCenter);
                edges.Add(edgeToCenter);
                vertexOfQuadCenter.Edges.Add(edgeToCenter);
                edgeCenter.Edges.Add(edgeToCenter);
                yield return StartCoroutine(LagOfAnimation(0.01f));
            }
        }
    }
    
    private IEnumerator RelaxVertices()
    {
        for (var i = 0; i < 300; i++)
        {
            foreach (var vertex in vertices)
            {
                if (vertex.RingIndex == ringCount - 1)
                {
                    continue;
                }

                var positionToChange = new Vector3();
                foreach (var edge in vertex.Edges)
                {
                    var directionEdge = (edge.Vertices.ToArray()[0] == vertex)
                        ? edge.Vertices.ToArray()[1]
                        : edge.Vertices.ToArray()[0];
                    
                    positionToChange += Vector3.Lerp(vertex.Position, directionEdge.Position, 0.05f);
                    
                }
                vertex.Position = positionToChange / vertex.Edges.Count;
            }
            
            yield return StartCoroutine(LagOfAnimation(0.000001f));
        }
    }

    [CanBeNull]
    private Vertex FindVertex(Vector3 position)
    {
        var vertex = vertices.Where(x => x.Position == position).ToArray();
        return vertex.Length > 0 ? vertex[0] : null;
    }

    [CanBeNull]
    private Edge FindEdge(Edge edge)
    {
        foreach (var existingEdge in edges)
        {
            if (Edge.IsEdgesSame(edge, existingEdge))
            {
                return existingEdge;
            }
        }

        return null;
    }
    
    private bool IsTriangleAlreadyExist(Triangle triangle)
    {
        foreach (var existingTriangle in triangles)
        {
            if (Triangle.IsTrianglesSame(triangle, existingTriangle))
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerator LagOfAnimation(float time)
    {
        yield return new WaitForSeconds(time);
    }

    private void OnDrawGizmos () {
        if (vertices == null) {
            return;
        }
        
        Gizmos.color = Color.yellow;
        foreach (var vertex in vertices)
        {
            Gizmos.DrawSphere(vertex.Position, 0.05f);
        }
        foreach (var edge in edges)
        {
            Gizmos.DrawLine(edge.Vertices.ToArray()[0].Position, edge.Vertices.ToArray()[1].Position);
        }
    }
}

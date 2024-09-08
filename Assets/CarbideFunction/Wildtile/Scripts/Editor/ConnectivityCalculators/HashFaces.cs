using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CarbideFunction.Wildtile;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile.Editor
{

internal static class HashFaces
{
    public static FaceLayoutHash FromFaceEdges(IEnumerable<Edge> faceEdges, int positionHashResolution, int normalHashResolution)
    {
        var hashValue = CombineFaceEdgeHashes(HashEdges(faceEdges, positionHashResolution, normalHashResolution));
        ImportInfoLog.Log(() => $"Hashing face edges: {hashValue}\n{StringifyFaceEdges(faceEdges, positionHashResolution, normalHashResolution)}");
        return FaceLayoutHash.FromRawInt(hashValue);
    }

    private static string StringifyFaceEdges(IEnumerable<Edge> faceEdges, int positionHashResolution, int normalHashResolution)
    {
        return String.Join(",\n", faceEdges.Select(edge => $"{{{edge.start} ({edge.startNormal.ToDetailedString()} quantized: {edge.startNormal.Quantize(normalHashResolution)}) - {edge.end} ({edge.endNormal.ToDetailedString()} quantized: {edge.endNormal.Quantize(normalHashResolution)})}}"));
    }

    public static FaceLayoutHash FromHorizontallyOppositeFaceEdges(IEnumerable<Edge> faceEdges, int positionHashResolution, int normalHashResolution)
    {
        var hashValue = CombineFaceEdgeHashes(HashHorizontallyOppositeEdges(faceEdges, positionHashResolution, normalHashResolution));
        ImportInfoLog.Log(() => $"Hashing horizontally opposite face edges: {hashValue}\n{StringifyFaceEdges(faceEdges, positionHashResolution, normalHashResolution)}");
        return FaceLayoutHash.FromRawInt(hashValue);
    }

    public static Edge HorizontallyInvertFaceEdge(Edge edge)
    {
        return new Edge{
            start = HorizontallyInvertVertex(edge.end),
            startNormal = HorizontallyInvertNormal(edge.endNormal),

            end = HorizontallyInvertVertex(edge.start),
            endNormal = HorizontallyInvertNormal(edge.startNormal),

            material = edge.material,
        };
    }

    public static Vector2 HorizontallyInvertVertex(Vector2 vertex)
    {
        return Vector2.Scale(vertex, new Vector2(-1f, 1f));
    }

    public static Vector3 HorizontallyInvertNormal(Vector3 normal)
    {
        return Vector3.Scale(normal, new Vector3(-1f, 1f, -1f));
    }

    public static FaceLayoutHash FromVerticallyOppositeFaceEdges(IEnumerable<Edge> faceEdges, int positionHashResolution, int normalHashResolution)
    {
        var hashValue = CombineFaceEdgeHashes(HashVerticallyOppositeEdges(faceEdges, positionHashResolution, normalHashResolution));
        ImportInfoLog.Log(() => $"Hashing vertically opposite face edges: {hashValue}\n{StringifyFaceEdges(faceEdges, positionHashResolution, normalHashResolution)}");
        return FaceLayoutHash.FromRawInt(hashValue);
    }

    public static Edge VerticallyInvertFaceEdge(Edge edge)
    {
        return new Edge{
            start = VerticallyInvertVertex(edge.end),
            startNormal = VerticallyInvertNormal(edge.endNormal),

            end = VerticallyInvertVertex(edge.start),
            endNormal = VerticallyInvertNormal(edge.startNormal),

            material = edge.material,
        };
    }

    public static Vector2 VerticallyInvertVertex(Vector2 vertex)
    {
        return Vector2.Scale(vertex, new Vector2(1f, -1f));
    }

    public static Vector3 VerticallyInvertNormal(Vector3 normal)
    {
        return Vector3.Scale(normal, new Vector3(1f, -1f, -1f));
    }

    public static IntegerType CombineFaceEdgeHashes(IEnumerable<IntegerType> edgeHashes)
    {
        return edgeHashes.Aggregate(0, (lhs, rhs) => lhs ^ rhs);
    }

    public static IEnumerable<IntegerType> HashHorizontallyOppositeEdges(IEnumerable<Edge> edges, int positionHashResolution, int normalHashResolution)
    {
        return edges.Select(edge => HashEdge(HorizontallyInvertFaceEdge(edge), positionHashResolution, normalHashResolution));
    }

    public static IEnumerable<IntegerType> HashVerticallyOppositeEdges(IEnumerable<Edge> edges, int positionHashResolution, int normalHashResolution)
    {
        return edges.Select(edge => HashEdge(VerticallyInvertFaceEdge(edge), positionHashResolution, normalHashResolution));
    }

    public static IEnumerable<IntegerType> HashEdges(IEnumerable<Edge> edges, int positionHashResolution, int normalHashResolution)
    {
        return edges.Select(edge => HashEdge(edge, positionHashResolution, normalHashResolution));
    }

    public static IntegerType HashEdge(Edge edge, int positionHashResolution, int normalHashResolution)
    {
        return Hash.Int(
               HashEdgePositions(edge, positionHashResolution)
             ^ HashEdgeNormals(edge, normalHashResolution)
             ^ HashEdgeMaterial(edge)
        );
    }

    public static IntegerType HashEdgePositions(Edge edge, int positionHashResolution)
    {
        return HashQuantizedPosition(edge.start, positionHashResolution)
             ^ Hash.CycleShift(HashQuantizedPosition(edge.end, positionHashResolution), 8);
    }

    public static IntegerType HashEdgeNormals(Edge edge, int normalHashResolution)
    {
        return Hash.CycleShift(HashQuantizedNormal(edge.startNormal, normalHashResolution), startNormalHashShift)
             ^ Hash.CycleShift(HashQuantizedNormal(edge.endNormal, normalHashResolution), endNormalHashShift);
    }

    public const int startNormalHashShift = 4;
    public const int endNormalHashShift = 12;

    public static IntegerType HashEdgeMaterial(Edge edge)
    {
        return Hash.CycleShift(EditorHash.AssetReference(edge.material), 5);
    }

    public static IntegerType HashQuantizedPosition(Vector2 vector, int hashResolution)
    {
        return Hash.QuantizedVector(vector, hashResolution);
    }

    private static IntegerType HashQuantizedNormal(Vector3 vector, int hashResolution)
    {
        return Hash.QuantizedVector(vector, hashResolution);
    }
}

}

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal static class TrianglesOnModuleFaceMeshCreator
{
    /// <summary>
    /// Creates a new mesh that highlights all the triangles on the face
    /// </summary>
    /// <param name="trianglesOnModuleBoundsFace">The triangles on the face for this module</param>
    /// <param name="originalMesh">The module's mesh. This must be the same mesh that was used to generate the <paramref name="trianglesOnModuleBoundsFace"/>s.</param>
    public static Mesh CreateMesh(
        IEnumerable<ModelTileConnectivityCalculator.ModuleConnectivityData.MeshTriangleOnBoundsFace> trianglesOnModuleBoundsFace,
        ModuleMesh originalMesh
    )
    {
        var triangles = GetMeshTriangleVerticesAndNormalsWithOutwardWinding(trianglesOnModuleBoundsFace, originalMesh);
        var triangleHighlights = CalculateHighlights(triangles);
        var mesh = new Mesh();
        mesh.vertices = triangleHighlights.SelectMany(tri => tri.TriVertices.Concat(tri.borderVertices.vertices)).ToArray();
        mesh.normals = triangleHighlights.SelectMany(tri => Enumerable.Repeat(tri.normal, 3).Concat(tri.borderVertices.normals)).ToArray();
        mesh.SetUVs(0, triangleHighlights.SelectMany(tri => Enumerable.Repeat(Vector3.zero, 3).Concat(tri.borderVertices.uv0)).ToArray());
        mesh.subMeshCount = 2;

        var indexBias = 0;
        var triangleVertIndices = new List<int>();
        var borderVertIndices = new List<int>();
        foreach (var tri in triangleHighlights)
        {
            triangleVertIndices.AddRange(new []{0 + indexBias, 1 + indexBias, 2 + indexBias});
            borderVertIndices.AddRange(CreateTrianglesForBorderStrip(indexBias + 3));
            indexBias += 3 + tri.borderVertices.vertices.Count;
        }

        mesh.SetIndices(triangleVertIndices.ToArray(), MeshTopology.Triangles, 0);
        mesh.SetIndices(borderVertIndices.ToArray(), MeshTopology.Triangles, 1);

        return mesh;
    }

    private class TriangleOnBounds
    {
        public Vector3 vertex0;
        public Vector3 vertex1;
        public Vector3 vertex2;

        public Vector3 normal;
    }

    private class TriangleHighlight
    {
        public Vector3 vertex0;
        public Vector3 vertex1;
        public Vector3 vertex2;

        public IEnumerable<Vector3> TriVertices
        {
            get{
                yield return vertex0;
                yield return vertex1;
                yield return vertex2;
            }
        }

        public MeshFragment borderVertices = new MeshFragment();

        public Vector3 normal;
    }

    private class MeshFragment
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> normals = new List<Vector3>();
        public List<Vector3> uv0 = new List<Vector3>();
    }

    private static IEnumerable<TriangleOnBounds> GetMeshTriangleVerticesAndNormalsWithOutwardWinding
    (
        IEnumerable<ModelTileConnectivityCalculator.ModuleConnectivityData.MeshTriangleOnBoundsFace> trianglesOnModuleBoundsFace,
        ModuleMesh originalMesh
    )
    {
        var vertices = originalMesh.vertices;
        var result = trianglesOnModuleBoundsFace.Select(tri => new TriangleOnBounds{
            vertex0 = vertices[tri.vertex0],
            vertex1 = vertices[tri.vertex1],
            vertex2 = vertices[tri.vertex2],
            normal = tri.faceNormal,
        });

        foreach (var tri in result)
        {
            if (CalculateWinding(tri) == Winding.Clockwise)
            {
                var cachedVertex1 = tri.vertex1;
                tri.vertex1 = tri.vertex2;
                tri.vertex2 = tri.vertex1;
            }
        }

        return result;
    }

    private enum Winding
    {
        Clockwise,
        AntiClockwise,
    }

    private static Winding CalculateWinding(TriangleOnBounds triangle)
    {
        var edge01 = triangle.vertex1 - triangle.vertex0;
        var edge02 = triangle.vertex2 - triangle.vertex0;
        var triFacing = Vector3.Cross(edge01, edge02);

        // for degenerate triangles the tri facing will be 0
        // (or approximately 0 if floating point errors affect it)
        if (triFacing.sqrMagnitude < 1E-5f)
        {
            return Winding.AntiClockwise;
        }

        var triMatchingNormal = Vector3.Dot(triFacing.normalized, triangle.normal);

        Assert.IsTrue(Mathf.Abs(triMatchingNormal) > (1f - 1E-4f), $"Triangle's normal didn't match the face normal: {triMatchingNormal}, normal: {triangle.normal}");

        return triMatchingNormal > 0f ? Winding.AntiClockwise : Winding.Clockwise;
    }

    private static IEnumerable<TriangleHighlight> CalculateHighlights
    (
        IEnumerable<TriangleOnBounds> trianglesOnBounds
    )
    {
        return trianglesOnBounds.Select(CalculateOneTriangleHighlight);
    }

    private static TriangleHighlight CalculateOneTriangleHighlight(TriangleOnBounds triangleOnBounds)
    {
        return new TriangleHighlight{
            vertex0 = triangleOnBounds.vertex0,
            vertex1 = triangleOnBounds.vertex1,
            vertex2 = triangleOnBounds.vertex2,
            normal = triangleOnBounds.normal,
            borderVertices = CalculateTriangleBorder(triangleOnBounds),
        };
    }

    private static MeshFragment CalculateTriangleBorder(TriangleOnBounds triangleOnBounds)
    {
        var result = new MeshFragment();

        AddVertexCornerTrisToMeshFragment(triangleOnBounds.vertex0, triangleOnBounds.vertex1, triangleOnBounds.vertex2, triangleOnBounds.normal, result);
        AddVertexCornerTrisToMeshFragment(triangleOnBounds.vertex1, triangleOnBounds.vertex2, triangleOnBounds.vertex0, triangleOnBounds.normal, result);
        AddVertexCornerTrisToMeshFragment(triangleOnBounds.vertex2, triangleOnBounds.vertex0, triangleOnBounds.vertex1, triangleOnBounds.normal, result);

        return result;
    }

    private static void AddVertexCornerTrisToMeshFragment(Vector3 left, Vector3 center, Vector3 right, Vector3 normal, MeshFragment output)
    {
        var leftCenter = center - left;
        var leftExtrude = Vector3.Cross(leftCenter, normal).normalized;
        var centerRight = right - center;
        var rightExtrude = Vector3.Cross(centerRight, normal).normalized;

        var centerExtrude = (leftExtrude + rightExtrude).normalized;

        output.vertices.AddRange(new []{center, center, center, center});
        output.normals.AddRange(new []{normal, normal, normal, normal});
        output.uv0.AddRange(new []{Vector3.zero, leftExtrude, centerExtrude, rightExtrude});
    }

    private static IEnumerable<int> CreateTrianglesForBorderStrip(int indexBias)
    {
        return new []{
            0,2,1,
            0,3,2,

            0,4,3,
            3,4,5,

            4,6,5,
            4,7,6,
            
            4,8,7,
            8,7,9,

            8,10,9,
            8,11,10,

            8,0,11,
            11,1,0,
        }.Select(index => index + indexBias);
    }
}

}

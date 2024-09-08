using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Class holding methods for checking if triangles are on any of a module's faces.
///
/// Any tris that are on one of a module's faces can cause the importer to miscalculate the corner contents and the face-face connections.
/// </summary>
internal static class TriangleOnModuleFaceDetector
{
    private const float tolerance = 1E-3f;
    /// <summary>
    /// Returns true if <paramref name="offsetFromOrigin"/> is close enough to <paramref name="faceOffset"/> to be considered on the face.
    /// </summary>
    public static bool AreFloatsEqualOnFace(float left, float right)
    {
        return Mathf.Abs(left - right) <= tolerance;
    }

    public delegate void OnFoundFaceOnModuleBoundsFace(int vertexIndex0, int vertexIndex1, int vertexIndex2, Vector3 normal);

    /// <summary>
    /// This method finds all faces that lie on the tile's boundaries, calling <paramref name="reporterCallable"/> if/when it finds one.
    /// </summary>
    /// <remarks>
    /// The mesh-related <paramref name="vertices"/>, <paramref name="indexBuffer"/>, and <paramref name="subMesh"/> parameters are used instead of <see href="https://docs.unity3d.com/ScriptReference/Mesh.html">Unity's Mesh class</see> because this allows for optimisations as repeated accesses (e.g. <c>mesh.vertices</c>) allocate and populate a new array for each access.
    /// </remarks>
    /// <param name="indexBuffer">Indices for drawing polygons on the face. This is known as <see href="https://docs.unity3d.com/ScriptReference/Mesh-triangles.html">triangles</see> in Unity's Mesh class.</param>
    /// <param name="subMesh">The sub mesh to check.</param>
    public static void FindAndReportFacesOnModuleBoundsFace(
        Vector3[] vertices, int[] indexBuffer, ModuleMesh.SubMesh subMesh,
        Vector3 dimensions,
        OnFoundFaceOnModuleBoundsFace reporterCallable
    )
    {
        var triangles = (IList<int>)new ArraySegment<int>(indexBuffer, subMesh.startIndex, subMesh.indicesCount);

        for (var trianglesIndex = 0; trianglesIndex < triangles.Count; trianglesIndex += 3)
        {
            Assert.IsTrue(trianglesIndex + 2 < triangles.Count);

            var halfDimensions = dimensions * 0.5f;

            TestAgainstForeAndAftFacesAlongDirection(vertices, triangles, trianglesIndex, halfDimensions, Vector3.up, reporterCallable);
            TestAgainstForeAndAftFacesAlongDirection(vertices, triangles, trianglesIndex, halfDimensions, Vector3.right, reporterCallable);
            TestAgainstForeAndAftFacesAlongDirection(vertices, triangles, trianglesIndex, halfDimensions, Vector3.forward, reporterCallable);
        }
    }

    private static void TestAgainstForeAndAftFacesAlongDirection(Vector3[] vertices, IList<int> indexBuffer, int triangleIndex, Vector3 halfDimensions, Vector3 direction, OnFoundFaceOnModuleBoundsFace reporterCallable)
    {
        var halfDimensionAlongDirection = Vector3.Dot(halfDimensions, direction);
        var vertex0Position = vertices[indexBuffer[triangleIndex]];
        var vertex0AlongDirection = Vector3.Dot(vertex0Position, direction);

        if (AreFloatsEqualOnFace(vertex0AlongDirection, halfDimensionAlongDirection))
        {
            TestOtherVerticesAgainstSingleFace(vertices, indexBuffer, triangleIndex, halfDimensionAlongDirection, direction, reporterCallable);
        }
        else if (AreFloatsEqualOnFace(vertex0AlongDirection, -halfDimensionAlongDirection))
        {
            Assert.IsTrue(TestVertexAgainstSingleFace(vertices, indexBuffer, triangleIndex, halfDimensionAlongDirection, -direction));
            TestOtherVerticesAgainstSingleFace(vertices, indexBuffer, triangleIndex, halfDimensionAlongDirection, -direction, reporterCallable);
        }
    }

    private static void TestOtherVerticesAgainstSingleFace(Vector3[] vertices, IList<int> indexBuffer, int triangleIndex, float halfDimensionAlongDirection, Vector3 direction, OnFoundFaceOnModuleBoundsFace reporterCallable)
    {
        if (   TestVertexAgainstSingleFace(vertices, indexBuffer, triangleIndex+1, halfDimensionAlongDirection, direction)
            && TestVertexAgainstSingleFace(vertices, indexBuffer, triangleIndex+2, halfDimensionAlongDirection, direction))
        {
            reporterCallable(indexBuffer[triangleIndex], indexBuffer[triangleIndex + 1], indexBuffer[triangleIndex + 2], direction);
        }
    }

    private static bool TestVertexAgainstSingleFace(Vector3[] vertices, IList<int> indexBuffer, int indexBufferIndex, float halfDimensionAlongDirection, Vector3 direction)
    {
        var vertex = vertices[indexBuffer[indexBufferIndex]];
        var vertexAlongDirection = Vector3.Dot(vertex, direction);
        return AreFloatsEqualOnFace(vertexAlongDirection, halfDimensionAlongDirection);
    }
}

}

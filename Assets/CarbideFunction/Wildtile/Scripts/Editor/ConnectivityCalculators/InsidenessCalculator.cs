using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class contains functions for determining whether a point is within an open mesh.
///
/// It is intended for use with the corners of voxel cubes, as seen in <see cref="Tileset"/>.
/// </summary>
internal static class InsidenessCalculator
{
    /// <summary>
    /// This function detects if a ray intersects with a triangle, and, if so, which side of the triangle it hits.
    ///
    /// This function uses pre-processed parameters so that the caller can precalculate the ray parameters before passing them to many functions. This obfuscates the code but also speeds it up.
    /// </summary>
    /// <param name="rayInverseOrientation">The quaternion that rotates the mesh into ray space, so that the ray points along the positive Z axis.</param>
    /// <param name="rayOffset">The vector that the ray starts at after rotating to ray space.</param>
    /// <param name="tri">The triangle to test against.</param>
    public static LineIntersectionResult LineSegmentIntersectionWithTri(Quaternion rayInverseOrientation, Vector3 rayOffset, Triangle tri)
    {
        var lineIntersection = LineIntersectionWithTri(rayInverseOrientation, (Vector2)rayOffset, tri);
        if (lineIntersection != LineIntersectionResult.Missed)
        {
            var lineIntersectionZDistanceFromPlane = GetLineIntersectionZDistance(rayInverseOrientation, rayOffset, tri);
            // if the winding is CCW, the normal will face the opposite direction
            var flipZDirection = (lineIntersection == LineIntersectionResult.HitCounterClockwise) ? -1f : 1f;

            if (lineIntersectionZDistanceFromPlane * flipZDirection < 0f)
            {
                return lineIntersection;
            }
        }

        return LineIntersectionResult.Missed;
    }

    public enum LineIntersectionResult
    {
        Missed,
        HitClockwise,
        HitCounterClockwise,
    }

    /// <summary>
    /// This function calculates the distance of the line to the contact point on the triangle.
    /// </summary>
    /// <param name="rayInverseOrientation">The quaternion that rotates the mesh into ray space, so that the ray points along the positive Z axis.</param>
    /// <param name="rayOffset">The vector that the ray starts at after rotating to ray space.</param>
    /// <param name="tri">The triangle to test against.</param>
    public static float GetLineIntersectionZDistance(Quaternion rayInverseOrientation, Vector3 rayOffset, Triangle tri)
    {
        // work in ray space because there're fewer chained calculations in these forward multiplies, rather than the Quaternion.Inverse then multiplying
        // This means there's less space for floating point errors to grow.
        var triVert0 = rayInverseOrientation * tri.vertex0;
        var triVert1 = rayInverseOrientation * tri.vertex1;
        var triVert2 = rayInverseOrientation * tri.vertex2;

        var offset01 = triVert1 - triVert0;
        var offset02 = triVert2 - triVert0;

        var triNormal = Vector3.Cross(offset01, offset02).normalized;

        var rayDistanceAlongNormal = Vector3.Dot(rayOffset, triNormal);
        var vert0DistanceAlongNormal = Vector3.Dot(triVert0, triNormal);

        return rayDistanceAlongNormal - vert0DistanceAlongNormal;
    }

    /// <summary>
    /// This function calculates whether a 2D point projected along the Z axis hits a triangle, and, if so, which side of the triangle it hits.
    /// </summary>
    /// <param name="rayInverseOrientation">The quaternion that rotates the mesh into ray space, so that the ray points along the positive Z axis.</param>
    /// <param name="ray2dOffset">The 2D vector to project the ray along the Z axis after rotating to ray space.</param>
    /// <param name="tri">The triangle to test against.</param>
    public static LineIntersectionResult LineIntersectionWithTri(Quaternion rayInverseOrientation, Vector2 ray2dOffset, Triangle tri)
    {
        return IsWithinTriangle(ray2dOffset, 
            (Vector2)(rayInverseOrientation * tri.vertex0),
            (Vector2)(rayInverseOrientation * tri.vertex1),
            (Vector2)(rayInverseOrientation * tri.vertex2));
    }

    /// <summary>
    /// This function calculates if a 2D point is within a 2D triangle.
    /// </summary>
    /// <returns>
    /// If two triangles share vertices but do not overlap, this function is guaranteed to return non-<see cref="LineIntersectionResult.Missed"/> for at most one of the triangles - there are no cases where a ray will hit two smooth triangles.
    /// </returns>
    public static LineIntersectionResult IsWithinTriangle(Vector2 queryPoint, Vector2 vertex0, Vector2 vertex1, Vector2 vertex2)
    {
        var inside01 = IsWithinHalfSpace(queryPoint, vertex0, vertex1);
        var inside12 = IsWithinHalfSpace(queryPoint, vertex1, vertex2);
        var inside20 = IsWithinHalfSpace(queryPoint, vertex2, vertex0);

        if (inside01 && inside12 && inside20)
        {
            return LineIntersectionResult.HitClockwise;
        }
        else if (!inside01 && !inside12 && !inside20)
        {
            return LineIntersectionResult.HitCounterClockwise;
        }

        return LineIntersectionResult.Missed;
    }

    /// <summary>
    /// This function calculates if a 2D point is within the half space defined by two vertices.
    /// </summary>
    /// <returns>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Argument state</term>
    ///     <description>Return value</description>
    ///   </listheader>
    ///   <item>
    ///     <term>The two vertices are the same</term>
    ///     <description>Returns true</description>
    ///   </item>
    ///   <item>
    ///     <term>The two vertices are different and not colinear with <paramref name="queryPoint"/></term>
    ///     <description>Returns true if <paramref name="queryPoint"/> is clockwise of <paramref name="vertex1"/> from <paramref name="vertex0"/>. Otherwise, returns false.</description>
    ///   </item>
    ///   <item>
    ///     <term>The two vertices are different and colinear with <paramref name="queryPoint"/></term>
    ///     <description>Returns true or false arbitrarily, but deterministically. Swapping <paramref name="vertex0"/> and <paramref name="vertex1"/> is guaranteed to give the opposite result.</description>
    ///   </item>
    /// </list>
    /// </returns>
    public static bool IsWithinHalfSpace(Vector2 queryPoint, Vector2 vertex0, Vector2 vertex1)
    {
        if (vertex0 == vertex1)
        {
            return true;
        }

        var shouldSwapVertices = ShouldSwapVertices(vertex0, vertex1);
        var vertex0Ordered = shouldSwapVertices ? vertex1 : vertex0;
        var vertex1Ordered = shouldSwapVertices ? vertex0 : vertex1;

        var tangent = vertex1Ordered - vertex0Ordered;
        var normal = new Vector2(tangent.y, -tangent.x);
        var delta = queryPoint - vertex0Ordered;
        var projection = Vector2.Dot(delta, normal);
        var isInMaybeSwappedHalfSpace = projection < 0f;

        // using != as XOR
        return isInMaybeSwappedHalfSpace != shouldSwapVertices;
    }

    private static bool ShouldSwapVertices(Vector2 vertex0, Vector2 vertex1)
    {
        if (vertex0.y != vertex1.y)
        {
            return vertex0.y > vertex1.y;
        }
        else
        {
            // if these are the exact same vertices then we will not swap them, but this will not make a difference in the algorithm
            return vertex0.x > vertex1.x;
        }
    }
}

}

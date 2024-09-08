using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarbideFunction.Wildtile.Editor
{

[System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
internal static class CornerContentsCalculator
{
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static void CalculateVertexContents
    (
        // functional arguments
        Matrix4x4 moduleTransform, Vector3[] vertices, int[] indexBuffer, IEnumerable<int> manifoldSubMeshIndices, ModuleMesh.SubMesh[] subMeshDescriptors, Vector3 modelDimensions, ModelTileConnectivityCalculator.ModuleConnectivityData output,
        // diagnostic arguments
        ModelTileConnectivityCalculator.VertexContentsInferenceDiagnostics diagnostics
    )
    {
        var triangleVertexIndicesSerialized = ModelTileConnectivityCalculator.GetManifoldTriangles(manifoldSubMeshIndices, indexBuffer, subMeshDescriptors);
        var triangles = triangleVertexIndicesSerialized
            .Select((vertexIndex, metaIndex) => new {vertexIndex, metaIndex})
            .GroupBy(enumeratedIndices => enumeratedIndices.metaIndex / 3, enumeratedIndices => enumeratedIndices.vertexIndex)
            .Select(vertexIndexTri => ModelTileConnectivityCalculator.VertexIndicesToTriangle(moduleTransform, vertices, vertexIndexTri))
            .ToList();

        var vertexContents = output.moduleVertexContents;

        for (var x = 0; x < 2; ++x)
        {
            for (var y = 0; y < 2; ++y)
            {
                for (var z = 0; z < 2; ++z)
                {
                    var vertexIndex = x + y*2 + z*4;
                    var cubeCornerVertex = Vector3.Scale(new Vector3(x, y, z) - Vector3.one * 0.5f, modelDimensions);
                    var startingTriIndex = triangles.FindIndex(tri => !IsVertexOnTriPlane(cubeCornerVertex, tri));
                    if (startingTriIndex != -1)
                    {
                        var startingTri = triangles[startingTriIndex];
                        var windingIsClockwise = IsWindingClockwise(cubeCornerVertex, startingTri);
                        diagnostics?.OnFirstTriSelected(vertexIndex, startingTri, windingIsClockwise);

                        var otherTris = triangles.Take(startingTriIndex).Concat(triangles.Skip(startingTriIndex + 1));

                        // custom is point inside polyhedron algorithm
                        // this deals with the specific case where the polyhedron is not a closed shape
                        var insideness = windingIsClockwise ? 1 : 0;

                        var startPoint = CenterOfTri(startingTri);

                        var rayInverseOrientation = Quaternion.Inverse(Quaternion.LookRotation(cubeCornerVertex - startPoint));
                        var rayOffset = rayInverseOrientation * startPoint;

                        foreach (var tri in otherTris)
                        {
                            switch (InsidenessCalculator.LineSegmentIntersectionWithTri(rayInverseOrientation, rayOffset, tri))
                            {
                                case InsidenessCalculator.LineIntersectionResult.HitClockwise:
                                    insideness -= 1;
                                    diagnostics?.OnTriCrossed(vertexIndex, tri, true);
                                    break;
                                case InsidenessCalculator.LineIntersectionResult.HitCounterClockwise:
                                    insideness += 1;
                                    diagnostics?.OnTriCrossed(vertexIndex, tri, false);
                                    break;
                                case InsidenessCalculator.LineIntersectionResult.Missed:
                                    diagnostics?.OnTriMissed(vertexIndex, tri);
                                    break;
                                default:
                                    Debug.Assert(false, $"Unexpected insideness value");
                                    break;
                            }
                        }

                        vertexContents[vertexIndex] = insideness;
                        diagnostics?.OnCubeCornerEvaluated(vertexIndex, insideness);
                    }
                    else
                    {
                        diagnostics?.OnNoValidTrianglesForVertex(vertexIndex);
                        foreach (var tri in triangles)
                        {
                            diagnostics?.OnTriMissed(vertexIndex, tri);
                        }
                        vertexContents[vertexIndex] = 0;
                    }
                }
            }
        }
    }

    private static bool IsVertexOnTriPlane(Vector3 vertex, Triangle tri)
    {
        var triNormal = Vector3.Cross(tri.vertex1 - tri.vertex0, tri.vertex2 - tri.vertex0);
        var triOffsetFromOriginAlongNormal = Vector3.Dot(triNormal, tri.vertex0);
        var vertexOffsetFromOriginAlongNormal = Vector3.Dot(triNormal, vertex);

        var difference = triOffsetFromOriginAlongNormal - vertexOffsetFromOriginAlongNormal;

        return Mathf.Abs(difference) < ModelTileConnectivityCalculator.floatComparisonTolerance;
    }

    private static bool IsWindingClockwise(Vector3 cameraVertex, Triangle tri)
    {
        var triNormal = Vector3.Cross(tri.vertex1 - tri.vertex0, tri.vertex2 - tri.vertex1);
        var offsetFromSecondToCamera = cameraVertex - tri.vertex2;
        return Vector3.Dot(triNormal, offsetFromSecondToCamera) < 0f;
    }

    private static Vector3 CenterOfTri(Triangle tri)
    {
        return tri.Aggregate(Vector3.zero, (a, b) => a + b) / 3f;
    }
}

}

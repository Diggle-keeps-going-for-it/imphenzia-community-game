using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class calculates the connections between models that will be used for modules.
/// </summary>
internal static class ModelTileConnectivityCalculator
{
    /// <summary>
    /// This class contains the edges on a module's face.
    /// </summary>
    public class ConnectiveEdges
    {
        public List<Edge> edges = new List<Edge>();
    }

    /// <summary>
    /// This class contains all the calculated data about a module, including the connections and cube corner contents.
    /// </summary>
    public class ModuleConnectivityData
    {
        public FaceData<ConnectiveEdges> connectiveFaces = FaceData<ConnectiveEdges>.Create(() => new ConnectiveEdges());

        // bit index
        // --X 0 = left, 1 = right
        // -X- 0 = down, 1 = up
        // X-- 0 = back, 1 = forward
        public int[] moduleVertexContents = new int[8];

        public struct MeshTriangleOnBoundsFace
        {
            public int vertex0;
            public int vertex1;
            public int vertex2;

            public Vector3 faceNormal;
        }

        public List<MeshTriangleOnBoundsFace> trianglesOnModuleBoundsFace = new List<MeshTriangleOnBoundsFace>();

        public List<int> vertexIndicesOutsideOfBounds = new List<int>();
    }

    /// <summary>
    /// This abstract class is an interface to receive calculation diagnostics. <see cref="GetConnectivityFromPrefab"/> calls the methods while calculating the connectivity.
    /// </summary>
    public abstract class VertexContentsInferenceDiagnostics
    {
        /// <summary>
        /// For each face, either this or <see cref="OnNoValidTrianglesForVertex"/> will be called at the start of processing.
        ///
        /// This method is called if, when searching a mesh, a triangle that is non-coplanar with the cube corner is found. The triangle will be used as the source of the insideness ray. 
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed</param>
        /// <param name="tri">The triangle that has been selected as the insideness ray source</param>
        /// <param name="isClockwise">Whether this triangle has clockwise winding. This is used to bias the insideness ray when it is cast through the model</param>
        public abstract void OnFirstTriSelected(int vertexIndex, Triangle tri, bool isClockwise);

        /// <summary>
        /// Called between calls to <see cref="OnFirstTriSelected"/> and <see cref="OnCubeCornerEvaluated"/> is called.
        ///
        /// This will be called if the insideness ray misses this triangle. If the ray hit this triangle, it would call <see cref="OnTriCrossed"/> instead.
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed</param>
        /// <param name="tri">The triangle that the insideness ray missed</param>
        public abstract void OnTriMissed(int vertexIndex, Triangle tri);

        /// <summary>
        /// Called between calls to <see cref="OnFirstTriSelected"/> and <see cref="OnCubeCornerEvaluated"/> is called.
        ///
        /// This will be called if the insideness ray hits this triangle. If the ray missed this triangle, it would call <see cref="OnTriCrossed"/> instead.
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed</param>
        /// <param name="tri">The triangle that the insideness ray hit</param>
        /// <param name="isClockwise">Whether the insideness ray hit the triangle with a clockwise winding</param>
        public abstract void OnTriCrossed(int vertexIndex, Triangle tri, bool isClockwise);

        /// <summary>
        /// Called if there are no triangles in the mesh, or if all the triangles that are in the mesh are coplanar with the current cube corner.
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed</param>
        public abstract void OnNoValidTrianglesForVertex(int vertexIndex);

        /// <summary>
        /// Called at the end of evaluating a single cube corner.
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed</param>
        /// <param name="insideness">How "inside" is the cube corner in the mesh. A value of 0 indicates this cube corner is outside, and a value of 1 indicates this cube corner is inside. Other values can be found if the model is non-manifold, which is not supported by the calculator</param>
        public abstract void OnCubeCornerEvaluated(int vertexIndex, int insideness);

        /// <summary>
        /// This user-callable function converts the vertexIndex parameter into a position on the unit cube. This is intended to be called within the abstract methods on this class, which all pass in the vertex index.
        /// </summary>
        /// <param name="vertexIndex">Which cube corner is currently being processed. Should be the value of the vertexIndex parameter from implemented versions of abstract methods on this class.</param>
        public static Vector3 GetVertexPosition(int vertexIndex)
        {
            return new Vector3(vertexIndex % 2, (vertexIndex / 2) % 2, (vertexIndex / 4) % 2) - Vector3.one * 0.5f;
        }
    }

    /// <summary>
    /// Calculate the connections for a model prefab. This includes each face's edges and whether the cube corners are inside or outside the model.
    /// </summary>
    /// <param name="prefab">The model prefab that will be </param>
    /// <param name="vertexContentsDiagnostics">If non-null, this instance will be called during the calculations as described in <see cref="VertexContentsInferenceDiagnostics"/> method descriptions</param>
    public static ModuleConnectivityData GetConnectivityFromMesh(
        ModuleMesh mesh, List<ImporterSettings.MaterialImportSettings> materialImportSettings, string moduleName,
        VertexContentsInferenceDiagnostics vertexContentsDiagnostics = null
    )
    {
        var connectivityData = new ModuleConnectivityData();
        var modelDimensions = Vector3.one;

        if (mesh != null && !mesh.IsEmpty())
        {
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var subMeshDescriptors = mesh.subMeshes;
            var indexBuffer = mesh.triangles;

            CacheConnectingEdges(vertices, normals, indexBuffer, subMeshDescriptors, Matrix4x4.identity, connectivityData, moduleName, materialImportSettings);
            var manifoldMaterials = materialImportSettings
                .Where(importSettings => importSettings.isPartOfManifoldMesh)
                .Select(importSettings => importSettings.targetMaterial)
                .ToList();
            CacheVertexContents(Matrix4x4.identity, vertices, indexBuffer, subMeshDescriptors, connectivityData, modelDimensions, manifoldMaterials, vertexContentsDiagnostics);
            CacheTrianglesOnFace(Matrix4x4.identity, vertices, indexBuffer, subMeshDescriptors, connectivityData, modelDimensions, manifoldMaterials);
            CacheVerticesOutsideOfBounds(Matrix4x4.identity, vertices, modelDimensions, connectivityData);
        }

        return connectivityData;
    }

    private static void CacheConnectingEdges(
        Vector3[] vertices,
        Vector3[] normals,
        int[] indexBuffer,
        ModuleMesh.SubMesh[] subMeshDescriptors,
        Matrix4x4 objectMatrix,
        ModuleConnectivityData output,
        string debugName,
        List<ImporterSettings.MaterialImportSettings> materialImportSettings
    )
    {
        foreach (var subMeshIndex in Enumerable.Range(0, subMeshDescriptors.Length))
        {
            var subMeshDescriptor = subMeshDescriptors[subMeshIndex];
            var material = subMeshDescriptor.material;
            var importSettings = materialImportSettings.FirstOrDefault(importSettings => importSettings.targetMaterial == material) ?? ImporterSettings.MaterialImportSettings.defaultSettings;

            if (importSettings.mustMatch)
            {
                var tris = (IList<int>)new ArraySegment<int>(indexBuffer, subMeshDescriptor.startIndex, subMeshDescriptor.indicesCount);

                var isFlipped = objectMatrix.determinant < 0f;

                for (var triangleStartVertexIndex = 0; triangleStartVertexIndex < tris.Count; triangleStartVertexIndex += 3)
                {
                    var vert0Index = tris[triangleStartVertexIndex    ];
                    var vert1Index = tris[triangleStartVertexIndex + 1];
                    var vert2Index = tris[triangleStartVertexIndex + 2];

                    var flipAwareVert0Index = vert0Index;
                    var flipAwareVert1Index = isFlipped ? vert2Index : vert1Index;
                    var flipAwareVert2Index = isFlipped ? vert1Index : vert2Index;

                    CacheConnectingEdgesFromTriangle(vertices, normals, objectMatrix, material, flipAwareVert0Index, flipAwareVert1Index, flipAwareVert2Index, importSettings.mustMatchNormalsOnBorder, output);
                }
            }
        }
    }

    private static void CacheConnectingEdgesFromTriangle(Vector3[] vertices, Vector3[] normals, Matrix4x4 matrix, Material material, int vertex0Index, int vertex1Index, int vertex2Index, bool mustMatchNormals, ModuleConnectivityData output)
    {
        AddEdgeIfOnFace(vertices, normals, matrix, vertex0Index, vertex1Index, material, output, mustMatchNormals);
        AddEdgeIfOnFace(vertices, normals, matrix, vertex1Index, vertex2Index, material, output, mustMatchNormals);
        AddEdgeIfOnFace(vertices, normals, matrix, vertex2Index, vertex0Index, material, output, mustMatchNormals);
    }

    /// <summary>
    /// This class describes a face in the context of calculating connections for it. It is used in <see cref="CacheConnectingEdges"/>, which reads from the full list of all 6 cube faces in <see cref="faceDefinitions"/>
    /// </summary>
    public class ConnectiveFaceDefinition
    {
        public enum OffsetDirection
        {
            Horizontal,
            Vertical,
        }

        public string name;
        public Quaternion orientation;
        public OffsetDirection offsetDirection;
        public Vector3Int gridOffset;
        public Face face;

        public override string ToString() => name;
    }
    /// <value>
    /// Contains populated <see cref="ConnectiveFaceDefinition">ConnectiveFaceDefinitions</see> for all 6 faces
    /// </value>
    public readonly static List<ConnectiveFaceDefinition> faceDefinitions = new List<ConnectiveFaceDefinition>{
        new ConnectiveFaceDefinition{
            name = "Forward",
            orientation = Quaternion.identity,
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Horizontal,
            gridOffset = Vector3Int.forward,
            face = Face.Forward,
        },
        new ConnectiveFaceDefinition{
            name = "Right",
            orientation = Quaternion.AngleAxis(90f, Vector3.up),
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Horizontal,
            gridOffset = Vector3Int.right,
            face = Face.Right,
        },
        new ConnectiveFaceDefinition{
            name = "Back",
            orientation = Quaternion.AngleAxis(180f, Vector3.up),
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Horizontal,
            gridOffset = Vector3Int.back,
            face = Face.Back,
        },
        new ConnectiveFaceDefinition{
            name = "Left",
            orientation = Quaternion.AngleAxis(270f, Vector3.up),
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Horizontal,
            gridOffset = Vector3Int.left,
            face = Face.Left,
        },
        new ConnectiveFaceDefinition{
            name = "Top",
            orientation = Quaternion.AngleAxis(-90f, Vector3.right),
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Vertical,
            gridOffset = Vector3Int.up,
            face = Face.Up,
        },
        new ConnectiveFaceDefinition{
            name = "Bottom",
            orientation = Quaternion.AngleAxis(90f, Vector3.right),
            offsetDirection = ConnectiveFaceDefinition.OffsetDirection.Vertical,
            gridOffset = Vector3Int.down,
            face = Face.Down,
        },
    };

    private static bool IsOnFace(Vector3 faceLocalVertex)
    {
        return TriangleOnModuleFaceDetector.AreFloatsEqualOnFace(faceLocalVertex.z, 0.5f);
    }

    private static void AddEdgeIfOnFace(Vector3[] vertices, Vector3[] normals, Matrix4x4 meshHolderTransform, int vertex0Index, int vertex1Index, Material material, ModuleConnectivityData outputFaces, bool useNormals)
    {
        var vertex0 = meshHolderTransform.MultiplyPoint(vertices[vertex0Index]);
        var vertex1 = meshHolderTransform.MultiplyPoint(vertices[vertex1Index]);

        foreach (var faceDefinition in faceDefinitions)
        {
            var vertex0FaceLocal = Quaternion.Inverse(faceDefinition.orientation) * vertex0;
            var vertex1FaceLocal = Quaternion.Inverse(faceDefinition.orientation) * vertex1;
            if (IsOnFace(vertex0FaceLocal) && IsOnFace(vertex1FaceLocal))
            {
                outputFaces.connectiveFaces[faceDefinition.face].edges.Add(CreateFaceEdge(
                    vertex0FaceLocal, vertex0Index,
                    vertex1FaceLocal, vertex1Index,
                    material,
                    meshHolderTransform,
                    faceDefinition,
                    normals,
                    useNormals
                ));
            }
        }
    }

    private static bool IsPopulated(Vector3[] normals)
    {
        return normals.GetLength(0) != 0;
    }

    private static Edge CreateFaceEdge
    (
        Vector3 vertex0FaceLocal, int vertex0Index,
        Vector3 vertex1FaceLocal, int vertex1Index,
        Material material,
        Matrix4x4 meshHolderTransform,
        ConnectiveFaceDefinition faceDefinition,
        Vector3[] normals,
        bool shouldUseNormalsIfAvailable
    )
    {
        var useNormals = shouldUseNormalsIfAvailable && IsPopulated(normals);
        
        if (useNormals)
        {
            return new Edge{
                start = (Vector2)vertex0FaceLocal,
                startNormal = Quaternion.Inverse(faceDefinition.orientation) * meshHolderTransform.MultiplyVector(normals[vertex0Index]),
                end = (Vector2)vertex1FaceLocal,
                endNormal = Quaternion.Inverse(faceDefinition.orientation) * meshHolderTransform.MultiplyVector(normals[vertex1Index]),
                material = material,
            };
        }
        else
        {
            return new Edge{
                start = (Vector2)vertex0FaceLocal,
                startNormal = Vector3.zero,
                end = (Vector2)vertex1FaceLocal,
                endNormal = Vector3.zero,
                material = material,
            };
        }
    }

    internal const float floatComparisonTolerance = 1E-3f;

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static bool DoMaterialsMatch(Edge left, Edge right)
    {
         return System.Object.ReferenceEquals(left.material, right.material);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static bool DoNormalsMatch(Edge left, Edge right)
    {
         return AreFlippedNormalsEqual(left.startNormal, right.endNormal)
             && AreFlippedNormalsEqual(left.endNormal, right.startNormal);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static Vector3 FlipNormalDirectionForMatchingFace(Vector3 targetFaceNormal)
    {
        return Vector3.Scale(targetFaceNormal, new Vector3(-1f, 1f, -1f));
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static bool AreFlippedNormalsEqual(Vector3 left, Vector3 right)
    {
        return (left - FlipNormalDirectionForMatchingFace(right)).sqrMagnitude <= floatComparisonTolerance * floatComparisonTolerance;
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static bool AreVerticesEqual(Vector2 left, Vector2 right, int positionHashResolution)
    {
        var leftHash = HashFaces.HashQuantizedPosition(left, positionHashResolution);
        var rightHash = HashFaces.HashQuantizedPosition(right, positionHashResolution);
        return leftHash == rightHash;
    }

    private static bool AreNormalsEqual(Vector3 leftNormal, Vector3 rightNormal)
    {
        return (leftNormal - rightNormal).sqrMagnitude <= floatComparisonTolerance * floatComparisonTolerance;
    }

    private const int unknownVertexContents = -144;

    private static void CacheVertexContents(Matrix4x4 moduleTransform, Vector3[] vertices, int[] indexBuffer, ModuleMesh.SubMesh[] subMeshDescriptors, ModuleConnectivityData output, Vector3 modelDimensions, List<Material> manifoldMaterials, VertexContentsInferenceDiagnostics diagnostics)
    {
        var manifoldSubMeshIndices = GetManifoldSubMeshIndices(subMeshDescriptors.Select(subMesh => subMesh.material).ToArray(), manifoldMaterials);
        CornerContentsCalculator.CalculateVertexContents(moduleTransform, vertices, indexBuffer, manifoldSubMeshIndices, subMeshDescriptors, modelDimensions, output, diagnostics);
    }

    private static void CacheTrianglesOnFace(Matrix4x4 modelTransform, Vector3[] vertices, int[] indexBuffer, ModuleMesh.SubMesh[] subMeshDescriptors, ModuleConnectivityData output, Vector3 modelDimensions, List<Material> manifoldMaterials)
    {
        var manifoldSubMeshIndices = GetManifoldSubMeshIndices(subMeshDescriptors.Select(subMesh => subMesh.material).ToArray(), manifoldMaterials);

        var triangleVertexIndicesSerialized = GetManifoldTriangles(manifoldSubMeshIndices, indexBuffer, subMeshDescriptors);
        var triangles = triangleVertexIndicesSerialized
            .Select((vertexIndex, metaIndex) => new {vertexIndex, metaIndex})
            .GroupBy(enumeratedIndices => enumeratedIndices.metaIndex / 3, enumeratedIndices => enumeratedIndices.vertexIndex)
            .Select(vertexIndexTri => VertexIndicesToTriangle(modelTransform, vertices, vertexIndexTri))
            .ToList();

        var trianglesOnFaces = output.trianglesOnModuleBoundsFace;

        foreach (var manifoldSubMeshIndex in manifoldSubMeshIndices)
        {
            var subMeshDescriptor = subMeshDescriptors[manifoldSubMeshIndex];
            TriangleOnModuleFaceDetector.FindAndReportFacesOnModuleBoundsFace(
                vertices, indexBuffer, subMeshDescriptor, modelDimensions,
                (vert0, vert1, vert2, normal) => trianglesOnFaces.Add(new ModuleConnectivityData.MeshTriangleOnBoundsFace{vertex0 = vert0, vertex1 = vert1, vertex2 = vert2, faceNormal = normal})
            );
        }
    }

    private static void CacheVerticesOutsideOfBounds(Matrix4x4 modelTransform, Vector3[] vertices, Vector3 modelDimensions, ModuleConnectivityData output)
    {
        var outsideVertices = output.vertexIndicesOutsideOfBounds;
        for (var vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
        {
            var rawVertexPosition = vertices[vertexIndex];
            var transformedVertexPosition = modelTransform.MultiplyPoint(rawVertexPosition);
            if (!IsPositionWithinTileBounds(transformedVertexPosition, modelDimensions))
            {
                outsideVertices.Add(vertexIndex);
            }
        }
    }

    private static bool IsPositionWithinTileBounds(Vector3 vertexPosition, Vector3 tileDimensions)
    {
        return IsPositionWithinTileBounds1d(vertexPosition.x, tileDimensions.x)
            && IsPositionWithinTileBounds1d(vertexPosition.y, tileDimensions.y)
            && IsPositionWithinTileBounds1d(vertexPosition.z, tileDimensions.z);
    }

    private const float withinTileBoundsEpsilon = 1e-5f;

    private static bool IsPositionWithinTileBounds1d(float vertexPosition, float tileDimension)
    {
        return Mathf.Abs(vertexPosition) <= tileDimension * 0.5f * (1f + withinTileBoundsEpsilon);
    }

    private static IEnumerable<int> GetManifoldSubMeshIndices(IEnumerable<Material> meshRendererMaterials, List<Material> importerDefinedManifoldMaterials)
    {
        if (importerDefinedManifoldMaterials.Count == 0)
        {
            return Enumerable.Range(0, meshRendererMaterials.Count());
        }
        else
        {
            return meshRendererMaterials
                .Select((mat, i) => new {mat, i})
                .Where(materialRecord => importerDefinedManifoldMaterials.Contains(materialRecord.mat))
                .Select(materialRecord => materialRecord.i);
        }
    }

    internal static int[] GetManifoldTriangles(IEnumerable<int> manifoldSubMeshIndices, int[] indexBuffer, ModuleMesh.SubMesh[] subMeshDescriptors)
    {
        return manifoldSubMeshIndices
            .SelectMany(subMeshIndex => indexBuffer.Skip(subMeshDescriptors[subMeshIndex].startIndex).Take(subMeshDescriptors[subMeshIndex].indicesCount))
            .ToArray();
    }

    internal static Triangle VertexIndicesToTriangle(Matrix4x4 transformMatrix, IEnumerable<Vector3> vertices, IEnumerable<int> triVertexIndices)
    {
        if (transformMatrix.determinant >= 0f)
        {
            return new Triangle{
                vertex0 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(0))),
                vertex1 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(1))),
                vertex2 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(2)))
            };
        }
        else
        {
            return new Triangle{
                vertex0 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(0))),
                vertex1 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(2))),
                vertex2 = transformMatrix.MultiplyPoint(vertices.ElementAt(triVertexIndices.ElementAt(1)))
            };
        }
    }

    public static ConnectiveFaceDefinition DirectionToConnectiveFace(Face direction)
    {
        return faceDefinitions[(int)direction];
    }

    private const float determinantTolerance = 1E-6f;
}

}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class manages a naive mesh for a voxel grid.
///
/// It can generate the naive mesh and can map any face index on the generated mesh to a voxel face. This is used by the tile map editor to select which voxel to fill/clear when clicking on the voxel grid.
/// It can also be used at runtime to test for mouse clicks in a live level editor.
///
/// The generated meshes contain faces for each of the filled-to-unfilled surfaces in the grid, as well as walls surrounding the whole grid.
/// </summary>
public class VoxelMesher
{
    /// <summary>
    /// Immediately spawn a temporary mesh GameObject, create a mesh, and assign the mesh to the new GameObject
    /// </summary>
    public void CreateMeshedVoxels
    (
        IVoxelGrid map,
        Vector3 tileDimensions,
        Material material,
        Material wallsMaterial,
        Material voxelTouchingBorderMaterial,
        Transform targetTransform,
        int layerIndex,
        HideFlags hideFlags = HideFlags.HideAndDontSave
    )
    {
        var generatedMesh = CreateAndGenerateMesh(map, tileDimensions);
        mesh = generatedMesh.mesh;
        trianglesToFaceData = generatedMesh.trianglesToFaceData;
        borderTrianglesToFaceData = generatedMesh.borderTrianglesToFaceData;
        voxelsTouchingBorderTrianglesToFaceData = generatedMesh.voxelsTouchingBorderTrianglesToFaceData;
        tempObject = CreateMeshObject(targetTransform, mesh, material, wallsMaterial, voxelTouchingBorderMaterial, layerIndex, hideFlags);
    }

    private GameObject CreateMeshObject(Transform pseudoParent, Mesh mesh, Material material, Material wallsMaterial, Material voxelTouchingBorderMaterial, int layerIndex, HideFlags hideFlags)
    {
        var newModelObject = CreateModelObject(mesh, material, wallsMaterial, voxelTouchingBorderMaterial, layerIndex, hideFlags);
        SceneManager.MoveGameObjectToScene(newModelObject, pseudoParent.gameObject.scene);

        // do manual "parenting" here so that this object isn't destroyed if you regenerate the map
        // (regenerating the map deletes all previous map tiles by iterating through all children)
        newModelObject.transform.position = pseudoParent.position;
        newModelObject.transform.localScale = pseudoParent.localScale;
        newModelObject.transform.localRotation = pseudoParent.rotation;

        return newModelObject;
    }

    /// <summary>
    /// Recreate the mesh and apply it to the already spawned GameObject.
    /// </summary>
    public void RegenerateMeshedVoxels(IVoxelGrid map, Vector3 tileDimensions)
    {
        GenerateAndPopulateMeshedVoxels(mesh, map, tileDimensions, trianglesToFaceData, borderTrianglesToFaceData, voxelsTouchingBorderTrianglesToFaceData);
        RefreshMeshCollider(mesh);
    }

    /// <summary>
    /// Swap the materials of the surfaces in the already spawned GameObject.
    /// </summary>
    public void ChangeMaterials(Material material, Material wallsMaterial, Material voxelsTouchingWallsMaterial)
    {
        if (tempObject != null)
        {
            var renderer = tempObject.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] {material, wallsMaterial, voxelsTouchingWallsMaterial};
        }
    }

    private void RefreshMeshCollider(Mesh mesh)
    {
        // required because otherwise the collider doesn't automatically add everything to the
        // physics world when the underlying mesh is changed

        // reapplying the mesh works in a scene but fails in prefab view
        var materials = SafeGetMaterialsFromPreviouslySpawnedMeshObject(tempObject);
        var hideFlags = tempObject.hideFlags;
        var newTempObject = CreateMeshObject(tempObject.transform, mesh, materials[0], materials[1], materials[2], tempObject.layer, hideFlags);
        UnityEngine.Object.DestroyImmediate(tempObject);
        tempObject = newTempObject;
    }

    private Material[] SafeGetMaterialsFromPreviouslySpawnedMeshObject(GameObject existingObject)
    {
        var meshRenderer = existingObject.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            return meshRenderer.sharedMaterials;
        }
        else
        {
            return new Material[]{null, null, null};
        }
    }

    /// <summary>
    /// Generates a simple mesh for the voxel grid, intended for use with level editors. It also populates <paramref name="trianglesToFaceDataToUpdate"/> with information about which coordinate is behind the tile and which is in front of the tile, making it easier to add or remove tiles when you click on them.
    ///
    /// The output includes two submeshes: Submesh zero holds the map's surface. Submesh one holds the map's boundary, facing inwards.
    /// </summary>
    /// <param name="meshToPopulate">Where the output mesh will be stored. Anything in the mesh currently will be overwritten.</param>
    /// <param name="map">A map of filled/empty cubes. This map's surface will be created and saved in the output <paramref name="meshToPopulate"/>.</param>
    /// <param name="trianglesToFaceDataToUpdate">An output list of <see cref="FaceData"/> that describes what coordinate is behind or in front of each mesh-triangle's face. The indices in this list match the tri indices from the mesh, so the caller can find which triangle a physics cast hits and look up that index in this list.</param>
    private static void GenerateAndPopulateMeshedVoxels(Mesh meshToPopulate, IVoxelGrid map, Vector3 tileDimensions, List<FaceData> trianglesToFaceDataToUpdate, List<FaceData> borderTrianglesToFaceDataToUpdate, List<FaceData> voxelsTouchingBorderTrianglesToFaceDataToUpdate)
    {
        Assert.IsNotNull(map);
        Assert.IsNotNull(map.Voxels);

        var vertices = new List<Vertex>();
        var triangleIndices = new List<int>();
        var borderTriangleIndices = new List<int>();
        var voxelsTouchingBorderTrianglesIndices = new List<int>();
        trianglesToFaceDataToUpdate.Clear();
        borderTrianglesToFaceDataToUpdate.Clear();
        voxelsTouchingBorderTrianglesToFaceDataToUpdate.Clear();

        foreach (var voxelIndex in Enumerable.Range(0, map.Voxels.Count()))
        {
            GenerateFacesForVoxel(voxelIndex, map, tileDimensions, vertices, triangleIndices, trianglesToFaceDataToUpdate, borderTriangleIndices, borderTrianglesToFaceDataToUpdate, voxelsTouchingBorderTrianglesIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
        }

        meshToPopulate.triangles = null;
        meshToPopulate.vertices = null;
        meshToPopulate.normals = null;
        meshToPopulate.uv = null;

        meshToPopulate.vertices = vertices.Select(vertex => vertex.position).ToArray();
        meshToPopulate.normals = vertices.Select(vertex => vertex.normal).ToArray();
        meshToPopulate.uv = vertices.Select(vertex => vertex.uv).ToArray();
        meshToPopulate.triangles = triangleIndices.Concat(borderTriangleIndices).Concat(voxelsTouchingBorderTrianglesIndices).ToArray();
        meshToPopulate.subMeshCount = 3;
        meshToPopulate.SetSubMesh(0, new SubMeshDescriptor{
            indexStart = 0,
            indexCount = triangleIndices.Count,
        }, MeshUpdateFlags.Default);
        meshToPopulate.SetSubMesh(1, new SubMeshDescriptor{
            indexStart = triangleIndices.Count,
            indexCount = borderTriangleIndices.Count,
        }, MeshUpdateFlags.Default);
        meshToPopulate.SetSubMesh(2, new SubMeshDescriptor{
            indexStart = triangleIndices.Count + borderTriangleIndices.Count,
            indexCount = voxelsTouchingBorderTrianglesIndices.Count,
        }, MeshUpdateFlags.Default);
    }

    /// <summary>
    /// This struct describes the voxel coordinates for a voxel face.
    /// </summary>
    public struct FaceData
    {
        /// <summary>
        /// The coordinate for the voxel behind this face. Should be cleared if you want to carve out this voxel face.
        /// </summary>
        public int voxelIndex;

        /// <summary>
        /// The coordinate for the voxel in front of this face. Should be set if you want to extrude this voxel face.
        /// </summary>
        public int facingVoxelIndex;

        /// <summary>
        /// Vertices that can be used to reconstruct this face e.g. for level editor highlights
        /// </summary>
        public Vertex[] faceVertices;
        /// <summary>
        /// Indices into the <see cref="faceVertices"/> array that can be used to reconstruct this face e.g. for level editor highlights
        /// </summary>
        public int[] faceIndices;
    }
    private List<FaceData> trianglesToFaceData = null;
    private List<FaceData> borderTrianglesToFaceData = null;
    private List<FaceData> voxelsTouchingBorderTrianglesToFaceData = null;

    /// <summary>
    /// Gets the <see cref="VoxelMesher.FaceData"/> for a triangle in the generated mesh. The mesh must have been generated first by calling <see cref="RegenerateMeshedVoxels"/>.
    ///
    /// The triangle index is expected to come from a <see href="https://docs.unity3d.com/ScriptReference/Collider.Raycast.html">physics raycast</see> based on where the user clicks.
    /// </summary>
    public FaceData GetFaceDataForTriIndex(int triangleIndex)
    {
        Assert.IsNotNull(trianglesToFaceData, "Requested face data when the mesh is not generated");
        if (triangleIndex < 0 && trianglesToFaceData.Count + borderTrianglesToFaceData.Count <= triangleIndex)
        {
            throw new ArgumentException(String.Format("Requested triangle index out of range. Requested {0}, the tri to voxel map size is {1}", triangleIndex, trianglesToFaceData.Count()));
        }

        Assert.IsTrue(triangleIndex >= 0);
        if (triangleIndex < trianglesToFaceData.Count)
        {
            return trianglesToFaceData[triangleIndex];
        }
        else
        {
            var borderTriangleIndex = triangleIndex - trianglesToFaceData.Count;
            if (borderTriangleIndex < borderTrianglesToFaceData.Count)
            {
                return borderTrianglesToFaceData[borderTriangleIndex];
            }
            else
            {
                var voxelFacingBorderTriangleIndex = borderTriangleIndex - borderTrianglesToFaceData.Count;
                Assert.IsTrue(voxelFacingBorderTriangleIndex < voxelsTouchingBorderTrianglesToFaceData.Count, $"Triangle index {triangleIndex} was out of range. default: {trianglesToFaceData.Count}, border: {borderTrianglesToFaceData.Count}, voxel to border: {voxelsTouchingBorderTrianglesToFaceData.Count}, total: {trianglesToFaceData.Count + borderTrianglesToFaceData.Count + voxelsTouchingBorderTrianglesToFaceData.Count}");
                return voxelsTouchingBorderTrianglesToFaceData[voxelFacingBorderTriangleIndex];
            }
        }
    }

    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 uv;

        public override string ToString()
        {
            return $"Vertex {position} (UV: {uv}, Normal: {normal})";
        }
    }

    /// <summary>
    /// Contains the output of a call to <see cref="CreateAndGenerateMesh"/>.
    /// </summary>
    public struct GeneratedMesh
    {
        /// <summary>
        /// A mesh containing the surface of a cuboid tile map in submesh 0 and the outer boundary of the map in submesh 1.
        /// </summary>
        public Mesh mesh;
        /// <summary>
        /// A list which contains the same number of items as the triangles for the main model of the mesh. If the triangle index is less than the length of this list, use the triangle's index to lookup the value in this list and get the voxel index behind the face and in front of the face.
        /// </summary>
        public List<FaceData> trianglesToFaceData;
        /// <summary>
        /// A list which contains the same number of items as the triangles for the border model in the mesh. Each triangle's index can be used to lookup in this list minus the length of the <see name="trianglesToFaceData"/> and get the voxel index behind the face and in front of the face.
        /// </summary>
        public List<FaceData> borderTrianglesToFaceData;
        /// <summary>
        /// A list which contains the same number of items as the triangles for the voxels touching the border model in the mesh. Each triangle's index can be used to lookup in this list minus the length of the <see name="trianglesToFaceData"/> + <see name="borderTrianglesToFaceData"/> and get the voxel index behind the face and in front of the face. These FaceData will only have voxels behind the face as the front faces out of the map.
        /// </summary>
        public List<FaceData> voxelsTouchingBorderTrianglesToFaceData;
    }

    /// <summary>
    /// Generates a simple mesh for the voxel grid, intended for use with level editors. It also populates <paramref name="trianglesToFaceDataToUpdate"/> with information about which coordinate is behind the tile and which is in front of the tile, making it easier to add or remove tiles when you click on them.
    ///
    /// The output includes two submeshes: Submesh zero holds the map's surface. Submesh one holds the map's boundary, facing inwards.
    /// </summary>
    /// <param name="map">A map of filled/empty cubes. This map's surface will be created and saved in the output <paramref name="meshToPopulate"/>.</param>
    /// <param name="tileDimensions">The size of the bounding box for an individual tile in the tileset</param>
    /// <returns>Returns the mesh and associated triangle data as a <see cref="GeneratedMesh"/>.</returns>
    public static GeneratedMesh CreateAndGenerateMesh(IVoxelGrid map, Vector3 tileDimensions)
    {
        var mesh = new Mesh();
        var trianglesToFaceData = new List<FaceData>();
        var borderTrianglesToFaceData = new List<FaceData>();
        var voxelsTouchingBorderTrianglesToFaceData = new List<FaceData>();
        GenerateAndPopulateMeshedVoxels(mesh, map, tileDimensions, trianglesToFaceData, borderTrianglesToFaceData, voxelsTouchingBorderTrianglesToFaceData);
        return new GeneratedMesh{mesh=mesh, trianglesToFaceData=trianglesToFaceData, borderTrianglesToFaceData=borderTrianglesToFaceData, voxelsTouchingBorderTrianglesToFaceData=voxelsTouchingBorderTrianglesToFaceData};
    }

    private class VoxelNeighbour
    {
        public Vector3Int coordinateDelta;
        public Quaternion rotation;
    }
    private static readonly List<VoxelNeighbour> voxelNeighbours = new List<VoxelNeighbour>{
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(0,0,1),
            rotation = Quaternion.identity
        },
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(0,0,-1),
            rotation = Quaternion.AngleAxis(180f, Vector3.up)
        },
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(1,0,0),
            rotation = Quaternion.AngleAxis(90f, Vector3.up)
        },
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(-1,0,0),
            rotation = Quaternion.AngleAxis(-90f, Vector3.up)
        },
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(0,1,0),
            rotation = Quaternion.AngleAxis(-90f, Vector3.right)
        },
        new VoxelNeighbour{
            coordinateDelta = new Vector3Int(0,-1,0),
            rotation = Quaternion.AngleAxis(90f, Vector3.right)
        },
    };

    private static void GenerateFacesForVoxel
    (
        int voxelIndex,
        IVoxelGrid map,
        Vector3 tileDimensions,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate,
        List<int> borderTriangleIndices,
        List<FaceData> borderTrianglesToFaceDataToUpdate,
        List<int> voxelsTouchingBorderTriangleIndices,
        List<FaceData> voxelsTouchingBorderTrianglesToFaceDataToUpdate
    )
    {
        GenerateTopFace(voxelIndex, map, tileDimensions, vertices, triangleIndices, trianglesToFaceDataToUpdate, borderTriangleIndices, borderTrianglesToFaceDataToUpdate, voxelsTouchingBorderTriangleIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
        GenerateBottomFace(voxelIndex, map, tileDimensions, vertices, triangleIndices, trianglesToFaceDataToUpdate, borderTriangleIndices, borderTrianglesToFaceDataToUpdate, voxelsTouchingBorderTriangleIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
        GenerateSideFaces(voxelIndex, map, tileDimensions, vertices, triangleIndices, trianglesToFaceDataToUpdate, borderTriangleIndices, borderTrianglesToFaceDataToUpdate, voxelsTouchingBorderTriangleIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
    }

    private static void GenerateTopFace
    (
        int voxelIndex,
        IVoxelGrid map,
        Vector3 tileDimensions,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate,
        List<int> borderTriangleIndices,
        List<FaceData> borderTrianglesToFaceDataToUpdate,
        List<int> voxelsTouchingBorderTriangleIndices,
        List<FaceData> voxelsTouchingBorderTrianglesToFaceDataToUpdate
    )
    {
        var voxel = map.GetVoxel(voxelIndex);
        if (voxel.contents != 0)
        {
            if (voxel.upNeighbourVoxelIndex == IVoxelGrid.outOfBoundsVoxelIndex)
            {
                AddUpFacingTriangles(voxel.upperYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, true, tileDimensions, voxelIndex, voxel.upNeighbourVoxelIndex, vertices, voxelsTouchingBorderTriangleIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
            }
            else
            {
                var upNeighbourVoxelContents = map.GetVoxel(voxel.upNeighbourVoxelIndex).contents;
                if (upNeighbourVoxelContents == 0)
                {
                    AddUpFacingTriangles(voxel.upperYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, true, tileDimensions, voxelIndex, voxel.upNeighbourVoxelIndex, vertices, triangleIndices, trianglesToFaceDataToUpdate);
                }
            }
        }
        else
        {
            var upNeighbourIsBorder = voxel.upNeighbourVoxelIndex == IVoxelGrid.outOfBoundsVoxelIndex;
            if (upNeighbourIsBorder)
            {
                AddDownFacingTriangles(voxel.upperYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, false, tileDimensions, IVoxelGrid.outOfBoundsVoxelIndex, voxelIndex, vertices, borderTriangleIndices, borderTrianglesToFaceDataToUpdate);
            }
        }
    }

    private static readonly int[] clockwiseWinding = new []{
        0,2,1,
        3,1,2,
    };
    private static readonly int[] counterClockwiseWinding = new []{
        0,1,2,
        3,2,1,
    };

    private static readonly int[] clockwiseSideWinding = new []{
        0,2,1,
        3,1,2,

        4,6,5,
        7,5,6,
    };
    private static readonly int[] counterClockwiseSideWinding = new []{
        0,1,2,
        3,2,1,

        4,5,6,
        7,6,5,
    };

    private static void AddUpFacingTriangles
    (
        float yCoordinate,
        Vector2 rawVoxelCenter,
        IVoxelGrid.VoxelFace[] voxelFaces,
        bool voxelIsFilled,
        Vector3 tileDimensions,
        int voxelIndex, 
        int upNeighbourVoxelIndex,
        List<Vertex> vertices, 
        List<int> triangleIndices, 
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var sharedFaceData = new FaceData{
            voxelIndex = voxelIndex,
            facingVoxelIndex = upNeighbourVoxelIndex,
        };
        var normal = Vector3.up;

        AddVerticalFacingTriangles(
            yCoordinate,
            rawVoxelCenter,
            voxelFaces,
            voxelIsFilled,
            tileDimensions,
            sharedFaceData,
            false,
            normal,
            vertices, 
            triangleIndices, 
            trianglesToFaceDataToUpdate
        );
    }

    private static void AddVerticalFacingTriangles
    (
        float yCoordinate,
        Vector2 rawVoxelCenter,
        IVoxelGrid.VoxelFace[] voxelFaces,
        bool voxelIsFilled,
        Vector3 tileDimensions,
        FaceData sharedFaceData, 
        bool useClockwiseVertexOrdering,
        Vector3 normal,
        List<Vertex> vertices, 
        List<int> triangleIndices, 
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        // create a quad for each corner between the voxel center, the halfway point of corner-1 to corner, corner, and the halfway point of corner to corner+1
        var lateralDimensionsScalar = new Vector2(tileDimensions.x, tileDimensions.z);
        var voxelCenter = Vector2.Scale(rawVoxelCenter, lateralDimensionsScalar);
        var scaledYCoordinate = yCoordinate * tileDimensions.y;

        var newFaceVerticesStartIndex = vertices.Count;
        var faceVertices = new List<Vertex>();
        var faceIndices = new List<int>();

        var voxelCenterIndex = faceVertices.Count;
        faceVertices.Add(new Vertex{
            position = CombineFlatBorderVertexAndHeight(voxelCenter, scaledYCoordinate),
            normal = normal,
            uv = new Vector2(0.5f, 0.5f),
        });

        for (var faceIndex = 0; faceIndex < voxelFaces.Length; ++faceIndex)
        {
            var startIndex = faceVertices.Count;

            var currentCorner = voxelFaces[faceIndex];

            var nextCornerIndex = (faceIndex + 1) % voxelFaces.Length;
            var nextCorner = voxelFaces[nextCornerIndex];
            AddVerticalFaceAndIndices(voxelCenterIndex, currentCorner, nextCorner, lateralDimensionsScalar, scaledYCoordinate, useClockwiseVertexOrdering, normal, faceVertices, faceIndices);
        }

        vertices.AddRange(faceVertices);
        triangleIndices.AddRange(faceIndices.Select(localVertexIndex => localVertexIndex + newFaceVerticesStartIndex));

        sharedFaceData.faceVertices = faceVertices.ToArray();
        sharedFaceData.faceIndices = faceIndices.ToArray();

        for (var faceIndex = 0; faceIndex < voxelFaces.Length; ++faceIndex)
        {
            trianglesToFaceDataToUpdate.Add(sharedFaceData);
            trianglesToFaceDataToUpdate.Add(sharedFaceData);
        }
    }

    private static int[] OptionallySwapIndices(bool shouldSwap, int first, int second)
    {
        if (shouldSwap)
        {
            return new []{second, first};
        }
        else
        {
            return new []{first, second};
        }
    }

    private class SimpleMesh
    {
        public Vector3[] vertices;
        public int[] indices;
    }

    private static void AddVerticalFaceVoxelCenterVertex
    (
        Vector2 scaledVoxelCenter,
        float scaledYCoordinate,
        List<Vector2> vertices
    )
    {
        vertices.Add(CombineFlatBorderVertexAndHeight(scaledVoxelCenter, scaledYCoordinate));
    }

    private static void AddVerticalFaceAndIndices
    (
        int voxelCenterIndex,
        IVoxelGrid.VoxelFace voxelFace,
        IVoxelGrid.VoxelFace nextVoxelFace,
        Vector2 lateralDimensionsScalar,
        float scaledYCoordinate,
        bool useClockwiseVertexOrdering,
        Vector3 normal,
        List<Vertex> vertices,
        List<int> indices
    )
    {
        // Assume the previous vertex corner's index index.
        // This assumption is invalid on first corner.
        // We cannot calculate it either, as there might be non-full voxel corners.
        // Do it simply, then wait for the end and populate it properly then.
        var edgeVertexIndex = vertices.Count;
        vertices.Add(new Vertex{
            position = CombineFlatBorderVertexAndHeight(Vector2.Scale(voxelFace.edgeMidpoint, lateralDimensionsScalar), scaledYCoordinate),
            normal = normal,
            uv = new Vector2(0.5f, 1f)
        });
        var cornerVertexIndex = vertices.Count;
        vertices.Add(new Vertex{
            position = CombineFlatBorderVertexAndHeight(Vector2.Scale(voxelFace.corner, lateralDimensionsScalar), scaledYCoordinate),
            normal = normal,
            uv = new Vector2(1f, 1f)
        });
        var nextEdgeMidpointVertexIndex = vertices.Count;
        vertices.Add(new Vertex{
            position = CombineFlatBorderVertexAndHeight(Vector2.Scale(nextVoxelFace.edgeMidpoint, lateralDimensionsScalar), scaledYCoordinate),
            normal = normal,
            uv = new Vector2(1f, 0.5f)
        });

        indices.AddRange(new int[]{}
            .Append(edgeVertexIndex)
            .Concat(OptionallySwapIndices(useClockwiseVertexOrdering, cornerVertexIndex, voxelCenterIndex))
            .Append(cornerVertexIndex)
            .Concat(OptionallySwapIndices(useClockwiseVertexOrdering, nextEdgeMidpointVertexIndex, voxelCenterIndex))
        );
    }

    private static void GenerateBottomFace
    (
        int voxelIndex,
        IVoxelGrid map,
        Vector3 tileDimensions,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate,
        List<int> borderTriangleIndices,
        List<FaceData> borderTrianglesToFaceDataToUpdate,
        List<int> voxelsTouchingBorderTriangleIndices,
        List<FaceData> voxelsTouchingBorderTrianglesToFaceDataToUpdate
    )
    {
        var voxel = map.GetVoxel(voxelIndex);
        if (voxel.contents != 0)
        {
            if (voxel.downNeighbourVoxelIndex == IVoxelGrid.outOfBoundsVoxelIndex)
            {
                AddDownFacingTriangles(voxel.lowerYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, true, tileDimensions, voxelIndex, voxel.downNeighbourVoxelIndex, vertices, voxelsTouchingBorderTriangleIndices, voxelsTouchingBorderTrianglesToFaceDataToUpdate);
            }
            else
            {
                var downNeighbourVoxelContents = map.GetVoxel(voxel.downNeighbourVoxelIndex).contents;
                if (downNeighbourVoxelContents == 0)
                {
                    AddDownFacingTriangles(voxel.lowerYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, true, tileDimensions, voxelIndex, voxel.downNeighbourVoxelIndex, vertices, triangleIndices, trianglesToFaceDataToUpdate);
                }
            }
        }
        else
        {
            var downNeighbourIsBorder = voxel.downNeighbourVoxelIndex == IVoxelGrid.outOfBoundsVoxelIndex;
            if (downNeighbourIsBorder)
            {
                AddUpFacingTriangles(voxel.lowerYCoordinate, voxel.voxelCenter, voxel.horizontalVoxelFaces, false, tileDimensions, IVoxelGrid.outOfBoundsVoxelIndex, voxelIndex, vertices, borderTriangleIndices, borderTrianglesToFaceDataToUpdate);
            }
        }
    }

    private static void AddDownFacingTriangles
    (
        float yCoordinate,
        Vector2 rawVoxelCenter,
        IVoxelGrid.VoxelFace[] voxelFaces,
        bool voxelIsFilled,
        Vector3 tileDimensions,
        int voxelIndex, 
        int downNeighbourVoxelIndex,
        List<Vertex> vertices, 
        List<int> triangleIndices, 
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var sharedFaceData = new FaceData{
            voxelIndex = voxelIndex,
            facingVoxelIndex = downNeighbourVoxelIndex,
        };
        var normal = Vector3.down;

        AddVerticalFacingTriangles(
            yCoordinate,
            rawVoxelCenter,
            voxelFaces,
            voxelIsFilled,
            tileDimensions,
            sharedFaceData,
            true,
            normal,
            vertices, 
            triangleIndices, 
            trianglesToFaceDataToUpdate
        );
    }

    private static void GenerateSideFaces
    (
        int voxelIndex,
        IVoxelGrid map,
        Vector3 tileDimensions,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate,
        List<int> borderTriangleIndices,
        List<FaceData> borderTrianglesToFaceDataToUpdate,
        List<int> voxelsTouchingBorderTriangleIndices,
        List<FaceData> voxelsTouchingBorderTrianglesToFaceDataToUpdate
    )
    {
        var voxel = map.GetVoxel(voxelIndex);
        var horizontalTileScaling = new Vector2(tileDimensions.x, tileDimensions.z);

        var lowerY = voxel.lowerYCoordinate * tileDimensions.y;
        var upperY = voxel.upperYCoordinate * tileDimensions.y;

        if (voxel.contents != 0)
        {
            for (var neighbourIndex = 0; neighbourIndex < voxel.horizontalVoxelFaces.Length; ++neighbourIndex)
            {
                var voxelFace = voxel.horizontalVoxelFaces[neighbourIndex];
                var neighbourVoxelIndex = voxelFace.facingVoxelIndex;
                var neighbourContents = neighbourVoxelIndex != IVoxelGrid.outOfBoundsVoxelIndex ? map.GetVoxel(neighbourVoxelIndex).contents : 0;

                var previousFaceIndex = (neighbourIndex + voxel.horizontalVoxelFaces.Length - 1) % voxel.horizontalVoxelFaces.Length;
                var previousFace = voxel.horizontalVoxelFaces[previousFaceIndex];
                var borderFaceData = new FaceData{voxelIndex = voxelIndex, facingVoxelIndex = IVoxelGrid.outOfBoundsVoxelIndex};
                var facingVoxelFaceData = new FaceData{voxelIndex = voxelIndex, facingVoxelIndex = neighbourVoxelIndex};

                if (neighbourContents == 0)
                {
                    if (previousFace.breakBetweenThisFaceAndNext)
                    {
                        if (voxelFace.breakBetweenThisFaceAndNext)
                        {
                            Debug.LogWarning("There is only one neighbour voxel on this neighbour island, unable to generate a face for this voxel");
                        }
                        else
                        {
                            GenerateMidToCornerSideFace(
                                lowerY, upperY,
                                0f,
                                voxel.lowerUvY, voxel.upperUvY,
                                neighbourIndex, voxel.horizontalVoxelFaces,
                                horizontalTileScaling,
                                facingVoxelFaceData, 1f, counterClockwiseWinding,
                                vertices,
                                triangleIndices,
                                trianglesToFaceDataToUpdate
                            );
                        }
                    }
                    else
                    {
                        if (voxelFace.breakBetweenThisFaceAndNext)
                        {
                            GeneratePrevCornerToMidSideFace(
                                lowerY, upperY,
                                0f,
                                voxel.lowerUvY, voxel.upperUvY,
                                neighbourIndex, voxel.horizontalVoxelFaces,
                                horizontalTileScaling,
                                facingVoxelFaceData, 1f, counterClockwiseWinding,
                                vertices,
                                triangleIndices,
                                trianglesToFaceDataToUpdate
                            );
                        }
                        else
                        {
                            GeneratePrevCornerToMidToCornerSideFace(
                                lowerY, upperY,
                                0f,
                                voxel.lowerUvY, voxel.upperUvY,
                                neighbourIndex, voxel.horizontalVoxelFaces,
                                horizontalTileScaling,
                                facingVoxelFaceData, 1f, counterClockwiseSideWinding,
                                vertices,
                                triangleIndices,
                                trianglesToFaceDataToUpdate
                            );
                        }
                    }
                }

                if (voxelFace.breakBetweenThisFaceAndNext)
                {
                    GenerateMidToCornerToNextMidSideFace(
                        lowerY, upperY,
                        0f,
                        voxel.lowerUvY, voxel.upperUvY,
                        neighbourIndex, voxel.horizontalVoxelFaces,
                        horizontalTileScaling,
                        borderFaceData, 1f, counterClockwiseSideWinding,
                        vertices,
                        voxelsTouchingBorderTriangleIndices,
                        voxelsTouchingBorderTrianglesToFaceDataToUpdate
                    );
                }
            }
        }
        else
        {
            var borderFaceData = new FaceData{voxelIndex = IVoxelGrid.outOfBoundsVoxelIndex, facingVoxelIndex = voxelIndex};
            for (var neighbourIndex = 0; neighbourIndex < voxel.horizontalVoxelFaces.Length; ++neighbourIndex)
            {
                var voxelFace = voxel.horizontalVoxelFaces[neighbourIndex];

                if (voxelFace.breakBetweenThisFaceAndNext)
                {
                    // draw border face facing inwards
                    GenerateMidToCornerToNextMidSideFace(
                        lowerY, upperY,
                        0f,
                        voxel.lowerUvY, voxel.upperUvY,
                        neighbourIndex, voxel.horizontalVoxelFaces,
                        horizontalTileScaling,
                        borderFaceData, -1f, clockwiseSideWinding,
                        vertices,
                        borderTriangleIndices,
                        borderTrianglesToFaceDataToUpdate
                    );
                }
            }
        }
    }

    private static void GenerateMidToCornerToNextMidSideFace
    (
        float lowerY,
        float upperY,
        float xOffset,
        float lowerUvY,
        float upperUvY,
        int faceIndex,
        IVoxelGrid.VoxelFace[] voxelFaces,
        Vector2 horizontalTileScaling,
        FaceData faceData,
        float normalScalar,
        int[] triangles,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var face = voxelFaces[faceIndex];
        var nextFaceIndex = (faceIndex + 1) % voxelFaces.Length;
        var nextFace = voxelFaces[nextFaceIndex];
        var flatBorderVertexLeft = Vector2.Scale(face.edgeMidpoint, horizontalTileScaling);
        var flatBorderVertexMid = Vector2.Scale(face.corner, horizontalTileScaling);
        var flatBorderVertexRight = Vector2.Scale(nextFace.edgeMidpoint, horizontalTileScaling);

        var borderVertexLeftLow   = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  lowerY);
        var borderVertexMidLow    = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   lowerY);
        var borderVertexRightLow  = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, lowerY);
        var borderVertexLeftHigh  = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  upperY);
        var borderVertexMidHigh   = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   upperY);
        var borderVertexRightHigh = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, upperY);

        GenerateFacesForHorizontalVoxelSide(
            borderVertexLeftHigh,
            borderVertexLeftLow,
            borderVertexMidHigh,
            borderVertexMidLow,
            borderVertexRightHigh,
            borderVertexRightLow,
            lowerUvY,
            upperUvY,
            xOffset,
            normalScalar,
            faceData, triangles,
            vertices, triangleIndices, trianglesToFaceDataToUpdate
        );
    }

    private static void GeneratePrevCornerToMidToCornerSideFace
    (
        float lowerY,
        float upperY,
        float xOffset,
        float lowerUvY,
        float upperUvY,
        int faceIndex,
        IVoxelGrid.VoxelFace[] voxelFaces,
        Vector2 horizontalTileScaling,
        FaceData faceData,
        float normalScalar,
        int[] triangles,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var face = voxelFaces[faceIndex];
        var prevFaceIndex = (faceIndex + voxelFaces.Length - 1) % voxelFaces.Length;
        var prevFace = voxelFaces[prevFaceIndex];
        var flatBorderVertexLeft = Vector2.Scale(prevFace.corner, horizontalTileScaling);
        var flatBorderVertexMid = Vector2.Scale(face.edgeMidpoint, horizontalTileScaling);
        var flatBorderVertexRight = Vector2.Scale(face.corner, horizontalTileScaling);

        var borderVertexLeftLow   = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  lowerY);
        var borderVertexMidLow    = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   lowerY);
        var borderVertexRightLow  = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, lowerY);
        var borderVertexLeftHigh  = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  upperY);
        var borderVertexMidHigh   = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   upperY);
        var borderVertexRightHigh = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, upperY);

        GenerateFacesForHorizontalVoxelSide(
            borderVertexLeftHigh,
            borderVertexLeftLow,
            borderVertexMidHigh,
            borderVertexMidLow,
            borderVertexRightHigh,
            borderVertexRightLow,
            lowerUvY,
            upperUvY,
            xOffset,
            normalScalar,
            faceData, triangles,
            vertices, triangleIndices, trianglesToFaceDataToUpdate
        );
    }

    private static void GeneratePrevCornerToMidSideFace
    (
        float lowerY,
        float upperY,
        float xOffset,
        float lowerUvY,
        float upperUvY,
        int faceIndex,
        IVoxelGrid.VoxelFace[] voxelFaces,
        Vector2 horizontalTileScaling,
        FaceData faceData,
        float normalScalar,
        int[] triangles,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var face = voxelFaces[faceIndex];
        var prevFaceIndex = (faceIndex + voxelFaces.Length - 1) % voxelFaces.Length;
        var prevFace = voxelFaces[prevFaceIndex];
        var flatBorderVertexLeft = Vector2.Scale(prevFace.corner, horizontalTileScaling);
        var flatBorderVertexMid = Vector2.Scale(face.edgeMidpoint, horizontalTileScaling);

        var borderVertexLeftLow   = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  lowerY);
        var borderVertexMidLow    = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   lowerY);
        var borderVertexLeftHigh  = CombineFlatBorderVertexAndHeight(flatBorderVertexLeft,  upperY);
        var borderVertexMidHigh   = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   upperY);

        GenerateFacesForQuad(
            borderVertexLeftLow,
            borderVertexMidLow,
            borderVertexLeftHigh,
            borderVertexMidHigh,
            0f + xOffset,
            lowerUvY,
            upperUvY,
            normalScalar,
            faceData, triangles,
            vertices, triangleIndices, trianglesToFaceDataToUpdate
        );
    }

    private static void GenerateMidToCornerSideFace
    (
        float lowerY,
        float upperY,
        float xOffset,
        float lowerUvY,
        float upperUvY,
        int faceIndex,
        IVoxelGrid.VoxelFace[] voxelFaces,
        Vector2 horizontalTileScaling,
        FaceData faceData,
        float normalScalar,
        int[] triangles,
        List<Vertex> vertices,
        List<int> triangleIndices,
        List<FaceData> trianglesToFaceDataToUpdate
    )
    {
        var face = voxelFaces[faceIndex];
        var prevFaceIndex = (faceIndex + voxelFaces.Length - 1) % voxelFaces.Length;
        var prevFace = voxelFaces[prevFaceIndex];
        var flatBorderVertexMid = Vector2.Scale(face.edgeMidpoint, horizontalTileScaling);
        var flatBorderVertexRight = Vector2.Scale(face.corner, horizontalTileScaling);

        var borderVertexMidLow    = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   lowerY);
        var borderVertexRightLow  = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, lowerY);
        var borderVertexMidHigh   = CombineFlatBorderVertexAndHeight(flatBorderVertexMid,   upperY);
        var borderVertexRightHigh = CombineFlatBorderVertexAndHeight(flatBorderVertexRight, upperY);

        GenerateFacesForQuad(
            borderVertexMidLow,
            borderVertexRightLow,
            borderVertexMidHigh,
            borderVertexRightHigh,
            0.5f + xOffset,
            lowerUvY,
            upperUvY,
            normalScalar,
            faceData, triangles,
            vertices, triangleIndices, trianglesToFaceDataToUpdate
        );
    }

    private static Vector3 CombineFlatBorderVertexAndHeight(Vector2 flatVertex, float height)
    {
        return new Vector3(flatVertex.x, height, flatVertex.y);
    }

    private static void GenerateFacesForHorizontalVoxelSide(
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Vector3 v5,
        float lowerUvY,
        float upperUvY,
        float uvXOffset,
        float normalScalar,
        FaceData faceData, int[] triangles,
        List<Vertex> vertices, List<int> triangleIndices, List<FaceData> trianglesToFaceData
    )
    {
        var leftQuadNormal = Vector3.Cross(v1 - v0, v2 - v0).normalized * normalScalar;
        var rightQuadNormal = Vector3.Cross(v3 - v2, v4 - v2).normalized * normalScalar;
        var localVertices = new List<Vertex>();
        localVertices.Add(new Vertex{position=v0, normal=leftQuadNormal, uv=new Vector2(0f   + uvXOffset, upperUvY)});
        localVertices.Add(new Vertex{position=v1, normal=leftQuadNormal, uv=new Vector2(0f   + uvXOffset, lowerUvY)});
        localVertices.Add(new Vertex{position=v2, normal=leftQuadNormal, uv=new Vector2(.5f  + uvXOffset, upperUvY)});
        localVertices.Add(new Vertex{position=v3, normal=leftQuadNormal, uv=new Vector2(.5f  + uvXOffset, lowerUvY)});
        localVertices.Add(new Vertex{position=v2, normal=rightQuadNormal, uv=new Vector2(.5f + uvXOffset, upperUvY)});
        localVertices.Add(new Vertex{position=v3, normal=rightQuadNormal, uv=new Vector2(.5f + uvXOffset, lowerUvY)});
        localVertices.Add(new Vertex{position=v4, normal=rightQuadNormal, uv=new Vector2(1f  + uvXOffset, upperUvY)});
        localVertices.Add(new Vertex{position=v5, normal=rightQuadNormal, uv=new Vector2(1f  + uvXOffset, lowerUvY)});

        Assert.AreEqual(trianglesToFaceData.Count(), triangleIndices.Count() / 3);
        Assert.AreEqual(triangles.Length, 12);

        var vertexIndexStart = vertices.Count();
        vertices.AddRange(localVertices);

        faceData.faceVertices = localVertices.ToArray();
        faceData.faceIndices = triangles;

        trianglesToFaceData.Add(faceData); 
        trianglesToFaceData.Add(faceData); 
        trianglesToFaceData.Add(faceData); 
        trianglesToFaceData.Add(faceData); 

        triangleIndices.AddRange(triangles.Select(baseIndex => vertexIndexStart + baseIndex));
    }

    private static void GenerateFacesForQuad(
        Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3,
        float uvXOffset,
        float lowerUvY,
        float upperUvY,
        float normalScalar,
        FaceData faceData, int[] triangles,
        List<Vertex> vertices, List<int> triangleIndices, List<FaceData> trianglesToFaceData
    )
    {
        var normal = Vector3.Cross(v1 - v0, v2 - v0).normalized * normalScalar;
        var localVertices = new List<Vertex>();
        localVertices.Add(new Vertex{position=v0, normal=normal, uv=new Vector2(0f  + uvXOffset,lowerUvY)});
        localVertices.Add(new Vertex{position=v1, normal=normal, uv=new Vector2(.5f + uvXOffset,lowerUvY)});
        localVertices.Add(new Vertex{position=v2, normal=normal, uv=new Vector2(0f  + uvXOffset,upperUvY)});
        localVertices.Add(new Vertex{position=v3, normal=normal, uv=new Vector2(.5f + uvXOffset,upperUvY)});

        Assert.AreEqual(trianglesToFaceData.Count(), triangleIndices.Count() / 3);
        Assert.AreEqual(triangles.Length, 6);

        var vertexIndexStart = vertices.Count();
        vertices.AddRange(localVertices);

        faceData.faceVertices = localVertices.ToArray();
        faceData.faceIndices = triangles;

        trianglesToFaceData.Add(faceData); 
        trianglesToFaceData.Add(faceData); 

        triangleIndices.AddRange(triangles.Select(baseIndex => vertexIndexStart + baseIndex));
    }

    private static int CalculateIndex(Vector3Int gridDimensions, Vector3Int coordinate)
    {
        return coordinate.x
            +  coordinate.y * gridDimensions.x
            +  coordinate.z * gridDimensions.x * gridDimensions.y;
    }

    /// <summary>
    /// Immediately destroy the spawned GameObject and release all references to the created mesh and creation data, allowing the C# GC to reclaim them.
    /// </summary>
    public void DestroyMeshedVoxels()
    {
        mesh = null;
        UnityEngine.Object.DestroyImmediate(tempObject);
        tempObject = null;
        trianglesToFaceData = null;
    }

    private static GameObject CreateModelObject(Mesh mesh, Material material, Material wallsMaterial, Material voxelTouchingBorderMaterial, int layerIndex, HideFlags hideFlags)
    {
        var allMaterialsValid = material != null && wallsMaterial != null && voxelTouchingBorderMaterial != null;
        var zeroMaterialsValid = material == null && wallsMaterial == null && voxelTouchingBorderMaterial == null;
        Assert.IsTrue(allMaterialsValid || zeroMaterialsValid, $"Either all materials should be supplied or no materials should be supplied. material was valid: {material != null}, wallsMaterial was valid: {wallsMaterial != null}, voxelTouchingBorderMaterial was valid: {voxelTouchingBorderMaterial != null}.");

        var newObject = new GameObject();
        newObject.layer = layerIndex;
        var collider = newObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;

        if (material != null)
        {
            var newFilter = newObject.AddComponent<MeshFilter>();
            newFilter.sharedMesh = mesh;
            AddRenderer(newObject, material, wallsMaterial, voxelTouchingBorderMaterial);
        }

        newObject.hideFlags = hideFlags;

        return newObject;
    }

    private static void AddRenderer(GameObject newObject, Material material, Material wallsMaterial, Material voxelTouchingBorderMaterial)
    {
        var renderer = newObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = new []{material, wallsMaterial, voxelTouchingBorderMaterial};
    }

    private Mesh mesh;
    private GameObject tempObject;

    /// <summary>
    /// Readonly access to the spawned GameObject.
    /// </summary>
    public GameObject TempMeshInstance => tempObject;
}

}

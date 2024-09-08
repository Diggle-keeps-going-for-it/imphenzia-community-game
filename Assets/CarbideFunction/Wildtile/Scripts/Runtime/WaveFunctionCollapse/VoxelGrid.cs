using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CarbideFunction.Wildtile
{

/// <summary>
/// A 3D grid of voxels that can be either "filled" or "empty".
/// </summary>
[CreateAssetMenu(fileName="New Voxel Grid", menuName= MenuConstants.topMenuName+"Voxel Grid", order=MenuConstants.orderBase+2)]
public class VoxelGrid : VoxelGridAsset
{
    [SerializeField]
    private int[] voxelData;
    /// <summary>
    /// Serialization name for voxelData.
    ///
    /// Not intended for users.
    /// </summary>
    public const string voxelDataName = nameof(voxelData);

    [SerializeField]
    internal int dimensionX;
    [SerializeField]
    internal int dimensionY;
    [SerializeField]
    internal int dimensionZ;

    /// <summary>
    /// Get the size of the 3D voxel grid.
    /// </summary>
    public Vector3Int Dimensions => new Vector3Int(dimensionX, dimensionY, dimensionZ);

    /// <summary>
    /// If enabled, the GridPlacer will use an extra layer of empty voxels at the top of the map. The GridPlacer will place end-caps on top of your models (if available in the tileset).
    ///
    /// If disabled, the GridPlacer will use the top layer of the map directly. This may leave some models uncapped.
    ///
    /// If you are creating a landscape or a standalone model, you probably want to set this flag.
    /// If you are creating a cave, you probably want to clear this flag.
    /// </summary>
    [SerializeField]
    [Tooltip("If enabled, the GridPlacer will use an extra layer of empty voxels at the top of the map. The GridPlacer will place end-caps on top of your models (if available in the tileset).")]
    public bool createTopTiles = true;

    /// <summary>
    /// If enabled, the GridPlacer will use an extra layer of empty voxels at the sides of the map. The GridPlacer will place end-caps on the sides of your models (if available in the tileset).
    ///
    /// If disabled, the GridPlacer will use the side layers of the map directly. This may leave some models uncapped.
    ///
    /// If you are creating a standalone model, you probably want to set this flag.
    /// If you are creating a cave, you probably want to clear this flag.
    /// </summary>
    [SerializeField]
    [Tooltip("If enabled, the GridPlacer will use an extra layer of empty voxels at the sides of the map. The GridPlacer will place end-caps on the sides of your models (if available in the tileset).")]
    public bool createSideTiles = true;

    /// <summary>
    /// If enabled, the GridPlacer will use an extra layer of empty voxels at the bottom of the map. The GridPlacer will place end-caps on the bottom of your models (if available in the tileset).
    ///
    /// If disabled, the GridPlacer will use the bottom layer of the map directly. This may leave some models uncapped.
    ///
    /// If you are creating a standalone model, you probably want to set this flag.
    /// If you are creating a cave or a landscape, you probably want to clear this flag.
    /// </summary>
    [SerializeField]
    [Tooltip("If enabled, the GridPlacer will use an extra layer of empty voxels at the bottom of the map. The GridPlacer will place end-caps on the bottom of your models (if available in the tileset).")]
    public bool createBottomTiles = true;

    /// <summary>
    /// Access a voxel in the grid.
    /// </summary>
    /// <returns>
    /// 1 if the voxel is filled, 0 otherwise.
    /// </returns>
    public int this[int x, int y, int z]
    {
        get => this[new Vector3Int(x,y,z)];
        set => this[new Vector3Int(x,y,z)] = value;
    }

    /// <summary>
    /// Access a voxel in the grid.
    /// </summary>
    /// <returns>
    /// 1 if the voxel is filled, 0 otherwise.
    /// </returns>
    public int this[Vector3Int coord]
    {
        get
        {
            EnsureVoxelDataValid();
            if (IsWithinBounds(coord))
            {
                return voxelData[coord.ToFlatArrayIndex(Dimensions)];
            }

            return 0;
        }

        set
        {
            EnsureVoxelDataValid();
            Assert.IsTrue(IsWithinBounds(coord));
            voxelData[coord.ToFlatArrayIndex(Dimensions)] = value;
        }
    }

    /// <summary>
    /// Access a voxel in the grid.
    /// </summary>
    /// <returns>
    /// 1 if the voxel is filled, 0 otherwise.
    /// </returns>
    public int this[int index]
    {
        get
        {
            EnsureVoxelDataValid();
            if (index >= 0 && index < voxelData.Length)
            {
                return voxelData[index];
            }

            return 0;
        }

        set
        {
            EnsureVoxelDataValid();
            Assert.IsTrue(index >= 0);
            Assert.IsTrue(index < voxelData.Length);
            voxelData[index] = value;
        }
    }

    /// <summary>
    /// Test if a coordinate is within the bounds of the voxel grid.
    ///
    /// If this is false, do not call <see cref="this[Vector3Int]"/>.
    /// </summary>
    public bool IsWithinBounds(Vector3Int coord)
    {
        return coord.x >= 0 && coord.x < Dimensions.x
            && coord.y >= 0 && coord.y < Dimensions.y
            && coord.z >= 0 && coord.z < Dimensions.z;
    }

    /// <summary>
    /// Gets the voxel at flat-index <paramref name="index"/>.
    ///
    /// Implementation of IVoxelGrid.GetVoxel.
    /// </summary>
    public override IVoxelGrid.Voxel GetVoxel(int index)
    {
        var inferredCoordinate = index.To3dCoordinate(Dimensions);
        Assert.IsTrue(IsWithinBounds(inferredCoordinate));
        var rootPosition = (Vector3)inferredCoordinate;
        var lateralRootPosition = new Vector2(rootPosition.x, rootPosition.z);

        var lowerUvY = (inferredCoordinate.y == 0) ? 0.5f : 0f;
        var upperUvY = (inferredCoordinate.y == Dimensions.y - 1) ? 0.5f : 1f;

        var lowerYCoordinate = rootPosition.y + lowerUvY;
        var upperYCoordinate = (float)rootPosition.y + upperUvY;

        return new IVoxelGrid.Voxel{
            contents = voxelData[index],
            upNeighbourVoxelIndex = SanitiseNeighbourAndConvertToIndex(inferredCoordinate + Vector3Int.up),
            downNeighbourVoxelIndex = SanitiseNeighbourAndConvertToIndex(inferredCoordinate + Vector3Int.down),
            lowerYCoordinate = lowerYCoordinate,
            upperYCoordinate = upperYCoordinate,
            lowerUvY = lowerUvY,
            upperUvY = upperUvY,
            voxelCenter = lateralRootPosition + centreOffsetFromRoot,
            horizontalVoxelFaces = ConstructVoxelFacesFromFaceOffsets(lateralRootPosition, inferredCoordinate),
        };
    }

    private IVoxelGrid.VoxelFace[] ConstructVoxelFacesFromFaceOffsets(Vector2 rootPosition, Vector3Int voxelCoordinate)
    {
        // might need to make the horizontalFaceOffsets a struct instead of a raw Vector2
        // and add the next part of the voxel to them e.g. (0f, 1f) for the corner, then (0.5f, 1f) for the mid point
        var result = new List<IVoxelGrid.VoxelFace>();
        for (var i = 0; i < horizontalFaceOffsets.Length; ++i)
        {
            var offset = horizontalFaceOffsets[i];

            var facingVoxelIndex = SanitiseNeighbourAndConvertToIndex(voxelCoordinate + offset.facingVoxelOffset);
            if (facingVoxelIndex != IVoxelGrid.outOfBoundsVoxelIndex)
            {
                var nextOffsetIndex = (i + 1) % horizontalFaceOffsets.Length;
                var nextOffset = horizontalFaceOffsets[nextOffsetIndex];
                var clockwiseVoxelIsInBounds = IsWithinBounds(voxelCoordinate + nextOffset.facingVoxelOffset);

                if (clockwiseVoxelIsInBounds)
                {
                    result.Add(new IVoxelGrid.VoxelFace(
                        rootPosition + offset.midPoint,
                        rootPosition + offset.corner,
                        facingVoxelIndex,
                        false
                    ));
                }
                else
                {
                    result.Add(new IVoxelGrid.VoxelFace(
                        rootPosition + offset.midPoint,
                        rootPosition + centreOffsetFromRoot,
                        facingVoxelIndex,
                        true
                    ));
                }
            }
        }
        return result.ToArray();
    }

    public override void SetVoxelContents(int index, int newContents)
    {
        Assert.IsTrue(index >= 0);
        Assert.IsTrue(index < voxelData.Length);
        voxelData[index] = newContents;
    }

    public override IReadOnlyList<IVoxelGrid.Voxel> Voxels
    { get {
        return Enumerable.Range(0, voxelData.Length).Select(voxelIndex => this.GetVoxel(voxelIndex)).ToList();
    }}

    private struct UnitFace
    {
        public Vector2 midPoint;
        public Vector2 corner;
        public Vector3Int facingVoxelOffset;

        public UnitFace(float midX, float midY, float cornerX, float cornerY, Vector3Int facingVoxelOffset)
        {
            midPoint = new Vector2(midX, midY);
            corner = new Vector2(cornerX, cornerY);
            this.facingVoxelOffset = facingVoxelOffset;
        }
    }
    private static readonly UnitFace[] horizontalFaceOffsets = new []{
        new UnitFace(0.5f, 1f  , 1f, 1f, Vector3Int.forward),
        new UnitFace(1f,   0.5f, 1f, 0f, Vector3Int.right),
        new UnitFace(0.5f, 0f  , 0f, 0f, Vector3Int.back),
        new UnitFace(0f,   0.5f, 0f, 1f, Vector3Int.left),
    };
    private static readonly Vector2 centreOffsetFromRoot = Vector2.one * 0.5f;

    internal int SanitiseNeighbourAndConvertToIndex(Vector3Int coordinate)
    {
        if (IsWithinBounds(coordinate))
        {
            return coordinate.ToFlatArrayIndex(Dimensions);
        }
        else
        {
            return IVoxelGrid.outOfBoundsVoxelIndex;
        }
    }

    private void EnsureVoxelDataValid()
    {
#if UNITY_EDITOR
        var voxelCountFromDimensions = Dimensions.GetFlatArraySize();
        if (voxelData == null)
        {
            RepairVoxelGrid(voxelCountFromDimensions);
        }
        else if (voxelData.GetLength(0) != voxelCountFromDimensions)
        {
            Debug.LogError("Voxel data is inconsistent with the dimensions. Replacing the data, this will lose your stored map");

            RepairVoxelGrid(voxelCountFromDimensions);
        }
#endif
    }

#if UNITY_EDITOR
    private void RepairVoxelGrid(int voxelCountFromDimensions)
    {
        Undo.RecordObject(this, "Repair voxel data");

        var thisSerialized = new SerializedObject(this);
        thisSerialized.FindProperty(voxelDataName).arraySize = voxelCountFromDimensions;
        thisSerialized.ApplyModifiedProperties();
    }
#endif

    /// <summary>
    /// Get the marching cube at the cube with its top-right-front corner at the specified coordinates.
    ///
    /// For example, if you requested the cube contents of (3,6,4), this method would query the contents of voxels:
    /// * (2,5,3)
    /// * (2,5,4)
    /// * (2,6,3)
    /// * (2,6,4)
    /// * (3,5,3)
    /// * (3,5,4)
    /// * (3,6,3)
    /// * (3,6,4)
    /// </summary>
    public int GetCubeContents(Vector3Int coords)
    {
        return
            Enumerable.Range(0,2).SelectMany(xAdd =>
            Enumerable.Range(0,2).SelectMany(yAdd =>
            Enumerable.Range(0,2).Select(    zAdd =>
            {
                return this[coords.x-1 + xAdd, coords.y-1 + yAdd, coords.z-1 + zAdd] << (xAdd + 2*yAdd + 4*zAdd);
            }))).Sum();
    }

    /// <summary>
    /// Resize the 3D grid and clear all previous contents.
    /// </summary>
    public void SetDimensionsAndClearGrid(Vector3Int newDimensions)
    {
        dimensionX = newDimensions.x;
        dimensionY = newDimensions.y;
        dimensionZ = newDimensions.z;

        voxelData = new int[Dimensions.GetFlatArraySize()];
    }

    public override void Visit(IVoxelGridVisitor visitor)
    {
        visitor.VisitRectangularVoxelGrid(this);
    }
}

}

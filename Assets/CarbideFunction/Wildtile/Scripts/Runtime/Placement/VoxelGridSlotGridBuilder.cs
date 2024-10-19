using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
    internal static class VoxelGridSlotGridBuilder
    {
        public static (SlotGrid, Vector3) CreateSlotGrid
        (
            VoxelGrid voxelGrid,
            bool createBottomTiles,
            bool createSideTiles,
            bool createTopTiles,
            Tileset tileset
        )
        {
            Assert.AreNotEqual(voxelGrid.Dimensions.x, 0);
            Assert.AreNotEqual(voxelGrid.Dimensions.y, 0);
            Assert.AreNotEqual(voxelGrid.Dimensions.z, 0);

            var (dimensions, bias) = CalculateDimensionsAndBias(voxelGrid.Dimensions, createBottomTiles, createSideTiles, createTopTiles);
            var slotGrid = CreateEmptySlotGrid(dimensions);
            PopulateSlotGrid(voxelGrid, slotGrid, dimensions, bias, tileset);

            var rootPosition = Vector3.Scale((Vector3)bias, tileset.TileDimensions);

            return (slotGrid, rootPosition);
        }

        private static (Vector3Int, Vector3Int) CalculateDimensionsAndBias(Vector3Int voxelGridDimensions, bool createBottomTiles, bool createSideTiles, bool createTopTiles)
        {
            var innerDimensions = voxelGridDimensions - Vector3Int.one;
            var extraYDimensions = (createBottomTiles ? 1 : 0) + (createTopTiles ? 1 : 0);
            var extraXzDimensions = createSideTiles ? 2 : 0;
            var dimensions = innerDimensions + Vector3Int.up * extraYDimensions + new Vector3Int(1, 0, 1) * extraXzDimensions;

            var bias = (new Vector3Int(1, 0, 1) * (createSideTiles ? 0 : 1)) + (Vector3Int.up * (createBottomTiles ? 0 : 1));

            return (dimensions, bias);
        }

        private static SlotGrid CreateEmptySlotGrid(Vector3Int dimensions)
        {
            return CreateCuboidGrid(dimensions);
        }

        private static void PopulateSlotGrid(VoxelGrid voxelGrid, SlotGrid grid, Vector3Int dimensions, Vector3Int bias, Tileset tileset)
        {
            for (var x = 0; x < dimensions.x; ++x)
            {
                for (var y = 0; y < dimensions.y; ++y)
                {
                    for (var z = 0; z < dimensions.z; ++z)
                    {
                        var rootCoord = new Vector3Int(x, y, z) + bias;
                        var voxelContentsIndex = GetSlotContents(voxelGrid, rootCoord);

                        var slot = grid.GetSlot(x,y,z, dimensions);
                        slot.contentsIndex = voxelContentsIndex;

                        // note the minus - voxel000Index DOES go to the lower left back voxel.
                        slot.sourceVoxels.voxel000Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(1,1,1));
                        slot.sourceVoxels.voxel001Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(0,1,1));
                        slot.sourceVoxels.voxel010Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(1,0,1));
                        slot.sourceVoxels.voxel011Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(0,0,1));
                        slot.sourceVoxels.voxel100Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(1,1,0));
                        slot.sourceVoxels.voxel101Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(0,1,0));
                        slot.sourceVoxels.voxel110Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(1,0,0));
                        slot.sourceVoxels.voxel111Index = voxelGrid.SanitiseNeighbourAndConvertToIndex(rootCoord - new Vector3Int(0,0,0));

                        if (ValidateMarchingCubeConfig(tileset.MarchingCubeLookup, voxelContentsIndex, out var marchingCubeConfig, out var errorMessage))
                        {
                            slot.SetAvailableModules(new List<TransformedModule>(marchingCubeConfig.availableModules), marchingCubeConfig.startingSupportedFaceCounts);
                        }
                        else
                        {
                            Debug.LogError($"Cube config {voxelContentsIndex} was invalid: {errorMessage}");
                            slot.SetAvailableModules(new List<TransformedModule>(), FaceData<int[]>.Create(() => new int[]{}));
                        }
                    }
                }
            }
        }

        private static int GetSlotContents(VoxelGrid sourceGrid, Vector3Int coords)
        {
            return sourceGrid.GetCubeContents(coords);
        }

        private static bool ValidateMarchingCubeConfig(Tileset.CubeConfiguration[] lookup, int contentsIndex, out Tileset.CubeConfiguration outConfig, out string errorMessage)
        {
            outConfig = new Tileset.CubeConfiguration();
            errorMessage = null;

            if (contentsIndex < 0)
            {
                errorMessage = $"{nameof(contentsIndex)} was less than 0";
                return false;
            }
            else if (contentsIndex >= 256)
            {
                errorMessage = $"{nameof(contentsIndex)} was greater than the lookup array size, {lookup.Length} (should be 256)";
                return false;
            }

            outConfig = lookup[contentsIndex];

            if (outConfig.availableModules == null)
            {
                errorMessage = $"{nameof(outConfig.availableModules)} was null";
                return false;
            }

            foreach (var faceData in FaceDataSerialization.serializationFaces)
            {
                if (outConfig.startingSupportedFaceCounts[faceData.face] == null)
                {
                    errorMessage = $"{nameof(outConfig.startingSupportedFaceCounts)}[{faceData.facePropertyName}] was null";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Create a 3D array of Slots to the size of <paramref name="dimensions"/>.
        /// </summary>
        /// <param name="dimensions">The size of the 3D grid of slots</param>
        public static SlotGrid CreateCuboidGrid(Vector3Int dimensions)
        {
            Assert.IsTrue(dimensions.x > 0);
            Assert.IsTrue(dimensions.y > 0);
            Assert.IsTrue(dimensions.z > 0);

            var slotData = new Slot[dimensions.x * dimensions.y * dimensions.z];

            CreateGridSlots(slotData, dimensions);
            SetUpGridSlotEdges(slotData, dimensions);

            return SlotGrid.CreateGridManually(slotData);
        }

        static void CreateGridSlots(Slot[] slotData, Vector3Int dimensions)
        {
            var rightNormal = Vector3.right * (1f / dimensions.x);
            var upNormal = Vector3.up * (1f / dimensions.y);
            var forwardNormal = Vector3.forward * (1f / dimensions.z);
            for (var x = 0; x < dimensions.x; ++x)
            {
                for (var y = 0; y < dimensions.y; ++y)
                {
                    for (var z = 0; z < dimensions.z; ++z)
                    {
                        var arrayIndex = new Vector3Int(x, y, z).ToFlatArrayIndex(dimensions);
                        var basePosition = new Vector3(x, y, z);
                        slotData[arrayIndex] = new Slot{
                            v000 = basePosition + new Vector3(-0.5f, -0.5f, -0.5f),
                            normalX000 = rightNormal,
                            normalY000 = upNormal,
                            normalZ000 = forwardNormal,

                            v001 = basePosition + new Vector3( 0.5f, -0.5f, -0.5f),
                            normalX001 = rightNormal,
                            normalY001 = upNormal,
                            normalZ001 = forwardNormal,

                            v010 = basePosition + new Vector3(-0.5f,  0.5f, -0.5f),
                            normalX010 = rightNormal,
                            normalY010 = upNormal,
                            normalZ010 = forwardNormal,

                            v011 = basePosition + new Vector3( 0.5f,  0.5f, -0.5f),
                            normalX011 = rightNormal,
                            normalY011 = upNormal,
                            normalZ011 = forwardNormal,

                            v100 = basePosition + new Vector3(-0.5f, -0.5f,  0.5f),
                            normalX100 = rightNormal,
                            normalY100 = upNormal,
                            normalZ100 = forwardNormal,

                            v101 = basePosition + new Vector3( 0.5f, -0.5f,  0.5f),
                            normalX101 = rightNormal,
                            normalY101 = upNormal,
                            normalZ101 = forwardNormal,

                            v110 = basePosition + new Vector3(-0.5f,  0.5f,  0.5f),
                            normalX110 = rightNormal,
                            normalY110 = upNormal,
                            normalZ110 = forwardNormal,

                            v111 = basePosition + new Vector3( 0.5f,  0.5f,  0.5f),
                            normalX111 = rightNormal,
                            normalY111 = upNormal,
                            normalZ111 = forwardNormal,
                        };
                    }
                }
            }
        }

        static void SetUpGridSlotEdges(Slot[] slotData, Vector3Int dimensions)
        {
            for (var x = 0; x < dimensions.x; ++x)
            {
                for (var y = 0; y < dimensions.y; ++y)
                {
                    for (var z = 0; z < dimensions.z; ++z)
                    {
                        var slotIndex = new Vector3Int(x, y, z).ToFlatArrayIndex(dimensions);
                        if (x > 0)
                        {
                            var targetIndex = new Vector3Int(x-1, y, z).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Left] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Right};
                        }

                        if (x < dimensions.x - 1)
                        {
                            var targetIndex = new Vector3Int(x+1, y, z).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Right] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Left};
                        }

                        if (y > 0)
                        {
                            var targetIndex = new Vector3Int(x, y-1, z).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Down] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Up};
                        }

                        if (y < dimensions.y - 1)
                        {
                            var targetIndex = new Vector3Int(x, y+1, z).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Up] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Down};
                        }

                        if (z > 0)
                        {
                            var targetIndex = new Vector3Int(x, y, z-1).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Back] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Forward};
                        }

                        if (z < dimensions.z - 1)
                        {
                            var targetIndex = new Vector3Int(x, y, z+1).ToFlatArrayIndex(dimensions);
                            slotData[slotIndex].halfLoops[Face.Forward] = new SlotHalfLoop{targetSlotIndex = targetIndex, targetSlot = slotData[targetIndex], facingFaceOnTarget = Face.Back};
                        }
                    }
                }
            }
        }
    }
}

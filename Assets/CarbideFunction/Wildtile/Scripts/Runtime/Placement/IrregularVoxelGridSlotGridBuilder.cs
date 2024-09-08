using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
    internal static class IrregularVoxelGridSlotGridBuilder
    {
        public static (SlotGrid, Vector3) CreateSlotGrid
        (
            IrregularVoxelGrid voxelGrid,
            Tileset tileset
        )
        {
            var slotGrid = CreateInitialSlotGrid(voxelGrid, tileset);
            ConnectSlotGridHalfLoops(voxelGrid, slotGrid);
            PopulateSlotGrid(slotGrid, tileset);

            return (slotGrid, Vector3.up * tileset.TileDimensions.y * 0.5f);
        }

        private static SlotGrid CreateInitialSlotGrid(IrregularVoxelGrid voxelGrid, Tileset tileset)
        {
            Assert.AreEqual(voxelGrid.indices.Length % 4, 0);
            var quadCount = voxelGrid.indices.Length / 4;
            var slots = new Slot[quadCount * (voxelGrid.height - 1)];
            for (var i = 0; i < slots.Length; ++i)
            {
                var height = i / quadCount;
                var zeroLayerQuadIndex = i - height * quadCount;
                var firstIndexIndex = zeroLayerQuadIndex * 4;
                var quadV0 = voxelGrid.indices[firstIndexIndex    ];
                var quadV1 = voxelGrid.indices[firstIndexIndex + 1];
                var quadV2 = voxelGrid.indices[firstIndexIndex + 2];
                var quadV3 = voxelGrid.indices[firstIndexIndex + 3];

                var edgeFlow = NeighbourQuadJacobiCalculator.CalculateEdgeFlowForQuad(voxelGrid.vertices, voxelGrid.indices, zeroLayerQuadIndex, tileset.TileDimensions.x, tileset.TileDimensions.y);

                var sourceVoxels = new Slot.SourceVoxels(
                    quadV0 + (height    ) * voxelGrid.vertices.Length,
                    quadV1 + (height    ) * voxelGrid.vertices.Length,
                    quadV0 + (height + 1) * voxelGrid.vertices.Length,
                    quadV1 + (height + 1) * voxelGrid.vertices.Length,
                    quadV3 + (height    ) * voxelGrid.vertices.Length,
                    quadV2 + (height    ) * voxelGrid.vertices.Length,
                    quadV3 + (height + 1) * voxelGrid.vertices.Length,
                    quadV2 + (height + 1) * voxelGrid.vertices.Length
                );
                var voxelContentsIndex = voxelGrid.GetCubeContents(sourceVoxels);

                slots[i] = new Slot{
                    v000 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV0, height     ),
                    normalX000 = edgeFlow.normal00.GetColumn(0),
                    normalY000 = edgeFlow.normal00.GetColumn(1),
                    normalZ000 = edgeFlow.normal00.GetColumn(2),

                    v001 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV1, height     ),
                    normalX001 = edgeFlow.normal01.GetColumn(0),
                    normalY001 = edgeFlow.normal01.GetColumn(1),
                    normalZ001 = edgeFlow.normal01.GetColumn(2),

                    v010 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV0, height + 1f),
                    normalX010 = edgeFlow.normal00.GetColumn(0),
                    normalY010 = edgeFlow.normal00.GetColumn(1),
                    normalZ010 = edgeFlow.normal00.GetColumn(2),

                    v011 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV1, height + 1f),
                    normalX011 = edgeFlow.normal01.GetColumn(0),
                    normalY011 = edgeFlow.normal01.GetColumn(1),
                    normalZ011 = edgeFlow.normal01.GetColumn(2),

                    v100 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV3, height     ),
                    normalX100 = edgeFlow.normal10.GetColumn(0),
                    normalY100 = edgeFlow.normal10.GetColumn(1),
                    normalZ100 = edgeFlow.normal10.GetColumn(2),

                    v101 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV2, height     ),
                    normalX101 = edgeFlow.normal11.GetColumn(0),
                    normalY101 = edgeFlow.normal11.GetColumn(1),
                    normalZ101 = edgeFlow.normal11.GetColumn(2),

                    v110 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV3, height + 1f),
                    normalX110 = edgeFlow.normal10.GetColumn(0),
                    normalY110 = edgeFlow.normal10.GetColumn(1),
                    normalZ110 = edgeFlow.normal10.GetColumn(2),

                    v111 = CalculateSlotCornerPosition(voxelGrid.vertices, quadV2, height + 1f),
                    normalX111 = edgeFlow.normal11.GetColumn(0),
                    normalY111 = edgeFlow.normal11.GetColumn(1),
                    normalZ111 = edgeFlow.normal11.GetColumn(2),

                    halfLoops = new FaceData<SlotHalfLoop>(),
                    sourceVoxels = sourceVoxels,
                    contentsIndex = voxelContentsIndex,
                };
            }
            return SlotGrid.CreateGridManually(slots);
        }

        private static void PopulateSlotGrid(SlotGrid slotGrid, Tileset tileset)
        {
            foreach (var slot in slotGrid.SlotData)
            {
                if (ValidateMarchingCubeConfig(tileset.MarchingCubeLookup, slot.contentsIndex, out var marchingCubeConfig, out var errorMessage))
                {
                    slot.SetAvailableModules(marchingCubeConfig.availableModules, marchingCubeConfig.startingSupportedFaceCounts);
                }
                else
                {
                    Debug.LogError($"Cube config {slot.contentsIndex} was invalid: {errorMessage}");
                    slot.SetAvailableModules(new List<TransformedModule>(), FaceData<int[]>.Create(() => new int[]{}));
                }
            }
        }

        private static Vector3 CalculateSlotCornerPosition(Vector3[] vertices, int index, float height)
        {
            return vertices[index] + Vector3.up * height;
        }

        private static void ConnectSlotGridHalfLoops(IrregularVoxelGrid voxelGrid, SlotGrid slotGrid)
        {
            var totalFacesInVoxelGrid = voxelGrid.indices.Length / 4;
            for (var layerIndex = 0; layerIndex < voxelGrid.height - 2; ++layerIndex)
            {
                var firstSlotInLayerIndex = layerIndex * totalFacesInVoxelGrid;
                for (var singleLayerSlotIndex = 0; singleLayerSlotIndex < totalFacesInVoxelGrid; ++singleLayerSlotIndex)
                {
                    var thisLayerSlotIndex = firstSlotInLayerIndex + singleLayerSlotIndex;
                    var aboveLayerSlotIndex = thisLayerSlotIndex + totalFacesInVoxelGrid;

                    var slot = slotGrid.SlotData[thisLayerSlotIndex];
                    var slotAbove = slotGrid.SlotData[aboveLayerSlotIndex];

                    SetVerticalHalfLoops(slot, slotAbove);
                }
            }

            for (var singleLayerSlotIndex = 0; singleLayerSlotIndex < totalFacesInVoxelGrid; ++singleLayerSlotIndex)
            {
                SearchForHorizontallyAdjacentSlots(voxelGrid, singleLayerSlotIndex, slotGrid);
            }
        }

        private static void SetVerticalHalfLoops
        (
            Slot slotBelow,
            Slot slotAbove
        )
        {
            Assert.IsNotNull(slotBelow);
            Assert.IsNotNull(slotAbove);

            slotBelow.halfLoops.up = new SlotHalfLoop{
                targetSlot = slotAbove,
                facingFaceOnTarget = Face.Down,
            };

            slotAbove.halfLoops.down = new SlotHalfLoop{
                targetSlot = slotBelow,
                facingFaceOnTarget = Face.Up,
            };
        }

        private struct AdjacentSlot
        {
            public int slotIndex;
            public Face receivingFace;
        }

        private static void SearchForHorizontallyAdjacentSlots
        (
            IrregularVoxelGrid voxelGrid,
            int singleLayerSlotIndex,
            SlotGrid slotGrid
        )
        {
            var firstIndexIndex = singleLayerSlotIndex * 4;
            var quadV0 = voxelGrid.indices[firstIndexIndex    ];
            var quadV1 = voxelGrid.indices[firstIndexIndex + 1];
            var quadV2 = voxelGrid.indices[firstIndexIndex + 2];
            var quadV3 = voxelGrid.indices[firstIndexIndex + 3];

            SearchForSingleDirectionHorizontallyAdjacentSlot(voxelGrid, singleLayerSlotIndex, slotGrid, quadV0, quadV1, EdgeIndexToFace(0));
            SearchForSingleDirectionHorizontallyAdjacentSlot(voxelGrid, singleLayerSlotIndex, slotGrid, quadV1, quadV2, EdgeIndexToFace(1));
            SearchForSingleDirectionHorizontallyAdjacentSlot(voxelGrid, singleLayerSlotIndex, slotGrid, quadV2, quadV3, EdgeIndexToFace(2));
            SearchForSingleDirectionHorizontallyAdjacentSlot(voxelGrid, singleLayerSlotIndex, slotGrid, quadV3, quadV0, EdgeIndexToFace(3));
        }

        private static void SearchForSingleDirectionHorizontallyAdjacentSlot
        (
            IrregularVoxelGrid voxelGrid,
            int singleLayerSlotIndex,
            SlotGrid slotGrid,
            int edgeLeftIndex, int edgeRightIndex,
            Face direction
        )
        {
            var totalFacesInVoxelGrid = voxelGrid.indices.Length / 4;

            var maybeForwardAdjacentSlot = FindAdjacentVoxelEdge(voxelGrid, edgeLeftIndex, edgeRightIndex, singleLayerSlotIndex + 1);
            if (maybeForwardAdjacentSlot is AdjacentSlot forwardAdjacentSlot)
            {
                for (var layerIndex = 0; layerIndex < (voxelGrid.height - 1); ++layerIndex)
                {
                    var firstSlotInLayerIndex = layerIndex * totalFacesInVoxelGrid;
                    var sourceSlotIndex = firstSlotInLayerIndex + singleLayerSlotIndex;
                    var sourceSlot = slotGrid.SlotData[sourceSlotIndex];
                    var destinationSlotIndex = firstSlotInLayerIndex + forwardAdjacentSlot.slotIndex;
                    var destinationSlot = slotGrid.SlotData[destinationSlotIndex];

                    SetHorizontalHalfLoops(sourceSlot, direction, destinationSlot, forwardAdjacentSlot.receivingFace);
                }
            }
        }

        private static void SetHorizontalHalfLoops
        (
            Slot slot0,
            Face face0,
            Slot slot1,
            Face face1
        )
        {
            Assert.IsNotNull(slot0);
            Assert.IsNotNull(slot1);

            slot0.halfLoops[face0] = new SlotHalfLoop{
                targetSlot = slot1,
                facingFaceOnTarget = face1,
            };

            slot1.halfLoops[face1] = new SlotHalfLoop{
                targetSlot = slot0,
                facingFaceOnTarget = face0,
            };
        }

        private static AdjacentSlot? FindAdjacentVoxelEdge
        (
            IrregularVoxelGrid voxelGrid,
            int indexLeft,
            int indexRight,
            int startSearchingFaceIndex
        )
        {
            var numberOfFaces = voxelGrid.indices.Length / 4;

            for (var faceIndex = startSearchingFaceIndex; faceIndex < numberOfFaces; faceIndex++)
            {
                var faceStartIndexIndex = faceIndex * 4;
                for (var edgeIndex = 0; edgeIndex < 4; ++edgeIndex)
                {
                    var remoteIndexIndexLeft =  ( edgeIndex         ) + faceStartIndexIndex;
                    var remoteIndexIndexRight = ((edgeIndex + 1) % 4) + faceStartIndexIndex;

                    var remoteVertexIndexLeft = voxelGrid.indices[remoteIndexIndexLeft];
                    var remoteVertexIndexRight = voxelGrid.indices[remoteIndexIndexRight];

                    if (remoteVertexIndexLeft  == indexRight
                     && remoteVertexIndexRight == indexLeft )
                    {
                        return new AdjacentSlot{
                            slotIndex = faceIndex,
                            receivingFace = EdgeIndexToFace(edgeIndex),
                        };
                    }
                }
            }
            
            return null;
        }

        private static Face EdgeIndexToFace(int edgeIndex)
        {
            Assert.IsTrue(edgeIndex >= 0);
            Assert.IsTrue(edgeIndex < 4);
            /*
            var offset = 0;
            return (Face)((edgeIndex + offset) % 4);
            */
            switch (edgeIndex)
            {
                case 0:
                    return Face.Back;
                case 1:
                    return Face.Right;
                case 2:
                    return Face.Forward;
                case 3:
                    return Face.Left;
                default:
                    Assert.IsTrue(false, $"Unhandled edge index {edgeIndex}");
                    return Face.Up;
            }
        }

        private static int GetSlotContents(IrregularVoxelGrid sourceGrid, Slot.SourceVoxels sourceVoxels)
        {
            return sourceGrid.GetCubeContents(sourceVoxels);
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
    }
}

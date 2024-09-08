using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// This class can build <see cref="SlotGrid"/>s from any <see cref="IVoxelGrid"/>.
    ///
    /// This follows the double-dispatch pattern and decouples SlotGrid construction from the classes implementing IVoxelGrid. The IVoxelGrid implementors can focus on implementing IVoxelGrid.
    /// </summary>
    public class GridBuilderVisitor : IVoxelGridVisitor
    {
        public GridBuilderVisitor(Tileset tileset)
        {
            this.tileset = tileset;
        }

        /// <summary>
        /// Will be populated by visiting a voxel grid.
        /// </summary>
        public SlotGrid slotGrid;

        Vector3 rootPosition;
        public Vector3 RootPosition => rootPosition;

        Tileset tileset;
        
        void IVoxelGridVisitor.VisitRectangularVoxelGrid(VoxelGrid voxelGrid)
        {
            Assert.AreNotEqual(voxelGrid.Dimensions.x, 0);
            Assert.AreNotEqual(voxelGrid.Dimensions.y, 0);
            Assert.AreNotEqual(voxelGrid.Dimensions.z, 0);

            (this.slotGrid, this.rootPosition) = VoxelGridSlotGridBuilder.CreateSlotGrid(voxelGrid, voxelGrid.createBottomTiles, voxelGrid.createSideTiles, voxelGrid.createTopTiles, tileset);
        }

        void IVoxelGridVisitor.VisitIrregularVoxelGrid(IrregularVoxelGrid voxelGrid)
        {
            Assert.IsNotNull(voxelGrid);
            (slotGrid, rootPosition) = IrregularVoxelGridSlotGridBuilder.CreateSlotGrid(voxelGrid, tileset);
        }
    }
}

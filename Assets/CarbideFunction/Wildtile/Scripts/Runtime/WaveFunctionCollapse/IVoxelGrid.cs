using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Access to a voxel grid. Intended to be implemented by <see cref="VoxelGrid"/> and <see cref="IrregularVoxelGrid"/> only.
/// </summary>
public interface IVoxelGrid
{
    void Visit(IVoxelGridVisitor visitor);

    Voxel GetVoxel(int index);
    void SetVoxelContents(int index, int newContents);

    IReadOnlyList<Voxel> Voxels {get;}

    /// <summary>
    /// Placeholder value to indicate that a voxel is out of bounds.
    /// </summary>
    const int outOfBoundsVoxelIndex = -143;

    class Voxel
    {
        /// <summary>
        /// Whether this voxel is filled (1) or empty (0).
        /// </summary>
        public int contents;

        /// <summary>
        /// The index of the voxel above this voxel.
        /// </summary>
        public int upNeighbourVoxelIndex;

        /// <summary>
        /// The index of the voxel below this voxel.
        /// </summary>
        public int downNeighbourVoxelIndex;

        /// <summary>
        /// The floor height of the voxel.
        /// </summary>
        public float lowerYCoordinate;

        /// <summary>
        /// The ceiling height of the voxel.
        /// </summary>
        public float upperYCoordinate;

        /// <summary>
        /// The floor UV value for the voxel.
        /// </summary>
        public float lowerUvY;

        /// <summary>
        /// The ceiling UV value for the voxel.
        /// </summary>
        public float upperUvY;

        /// <summary>
        /// The horizontal middle of the voxel.
        /// </summary>
        public Vector2 voxelCenter;

        /// <summary>
        /// This list is ordered clockwise around this voxel when viewed from above. The vertex and the next bound the edge to the corresponding neighbour voxel (so 0-1 will draw the border edge to horizontal neighbour 0, 1-2 will draw the edge to horizontal neighbour 1).
        /// The final vertex and the zeroth vertex bound the edge to the final neighbour.
        /// </summary>
        public VoxelFace[] horizontalVoxelFaces;
    }

    /// <summary>
    /// This struct contains information about the quads that compose this voxel.
    /// </summary>
    public struct VoxelFace
    {
        /// <summary>
        /// A location between this corner and the next corner. Might not be exactly between the corners, and might not be in line.
        /// </summary>
        public Vector2 edgeMidpoint;

        /// <summary>
        /// The location of this corner of the voxel.
        /// </summary>
        public Vector2 corner;

        /// <summary>
        /// Which voxel does this face look towards?
        ///
        /// If the voxel is the out of bounds voxel then this face is not filled.
        /// </summary>
        public int facingVoxelIndex;

        /// <summary>
        /// Do the quads between this face's midpoint, this face's corner, and the next face's midpoint point out of bounds?
        /// </summary>
        public bool breakBetweenThisFaceAndNext;

        /// <summary>
        /// Basic constructor.
        /// </summary>
        public VoxelFace(Vector2 edgeMidpoint, Vector2 corner, int facingVoxelIndex, bool breakBetweenThisFaceAndNext)
        {
            this.edgeMidpoint = edgeMidpoint;
            this.corner = corner;
            this.facingVoxelIndex = facingVoxelIndex;
            this.breakBetweenThisFaceAndNext = breakBetweenThisFaceAndNext;
        }
    }
}
}

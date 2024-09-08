using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// Warps the normals from within a (-0.5 - 0.5) cube.
    /// 
    /// Intended to be constructed by Wildtile and then used by Postprocessors (including user-built ones) by accessing it through <see name="PostprocessableMap"/> slots.
    /// </summary>
    public struct NormalWarper
    {
        /// <summary>
        /// Pass the X, Y, and Z normal warp transforms at each corner.
        ///
        /// The parameters' numbers are in ZYX order, so 001 is the corner to the right of 000.
        /// </summary>
        public NormalWarper
        (
            Vector3 x000, Vector3 y000, Vector3 z000,
            Vector3 x001, Vector3 y001, Vector3 z001,
            Vector3 x010, Vector3 y010, Vector3 z010,
            Vector3 x011, Vector3 y011, Vector3 z011,
            Vector3 x100, Vector3 y100, Vector3 z100,
            Vector3 x101, Vector3 y101, Vector3 z101,
            Vector3 x110, Vector3 y110, Vector3 z110,
            Vector3 x111, Vector3 y111, Vector3 z111
        )
        {
            this.xWarper = new VertexWarper(
                x000,
                x001,
                x010,
                x011,
                x100,
                x101,
                x110,
                x111
            );
            this.yWarper = new VertexWarper(
                y000,
                y001,
                y010,
                y011,
                y100,
                y101,
                y110,
                y111
            );
            this.zWarper = new VertexWarper(
                z000,
                z001,
                z010,
                z011,
                z100,
                z101,
                z110,
                z111
            );
        }

        /// <summary>
        /// Captured warp at a point within the cube. This can be used to calculate the actual warped normal.
        /// </summary>
        public struct WarpPoint
        {
            internal WarpPoint
            (
                Vector3 x,
                Vector3 y,
                Vector3 z
            )
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }

            /// <summary>
            /// Convert a tile-space normal into a world-space normal
            /// </summary>
            public Vector3 WarpNormal(Vector3 originalNormal)
            {
                return (originalNormal.x * x + originalNormal.y * y + originalNormal.z * z);
            }

            Vector3 x;
            Vector3 y;
            Vector3 z;
        }

        /// <summary>
        /// Get the warp transform at a point in the cube. This warp transform can then convert tile-space normals into world-space normals.
        /// </summary>
        public WarpPoint GetWarperAtPoint(Vector3 position)
        {
            return new WarpPoint(
                xWarper.WarpPosition(position),
                yWarper.WarpPosition(position),
                zWarper.WarpPosition(position)
            );
        }

        /// <summary>
        /// Warper that returns the input normals unchanged.
        /// </summary>
        public static readonly NormalWarper identityWarper = new NormalWarper(
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward,
            Vector3.right, Vector3.up, Vector3.forward
        );

        VertexWarper xWarper;
        VertexWarper yWarper;
        VertexWarper zWarper;
    }
}

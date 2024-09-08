using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// Warps positional points from the (-.5, -.5, -.5) to (.5, .5, .5) cube to the cube defined in the constructor.
    /// </summary>
    public struct VertexWarper
    {
        /// <summary>
        /// Build a new VertexWarper by defining each corner's position
        ///
        /// The list is in ascending X-Y-Z order, and the parameters are named in ZYX order. v000 is the left bottom back corner. v001 is the RIGHT bottom back corner.
        /// v100 is the left bottom FRONT corner.
        /// </summary>
        public VertexWarper(
            Vector3 v000,
            Vector3 v001,
            Vector3 v010,
            Vector3 v011,
            Vector3 v100,
            Vector3 v101,
            Vector3 v110,
            Vector3 v111
        )
        {
            this.v000 = v000;
            this.v001 = v001;
            this.v010 = v010;
            this.v011 = v011;
            this.v100 = v100;
            this.v101 = v101;
            this.v110 = v110;
            this.v111 = v111;
        }

        /// <summary>
        /// Warp a position vector.
        /// </summary>
        /// <param name="vertex">
        /// A vertex with values in the range -0.5 - 0.5.
        ///
        /// Values outside the range will be warped too but may be more extreme than intended.
        /// </param>
        public Vector3 WarpPosition
        (
            Vector3 vertex
        )
        {
            var blendValue = vertex + Vector3.one * 0.5f;

            var e00x = Vector3.Lerp(v000, v001, blendValue.x);
            var e01x = Vector3.Lerp(v010, v011, blendValue.x);
            var e10x = Vector3.Lerp(v100, v101, blendValue.x);
            var e11x = Vector3.Lerp(v110, v111, blendValue.x);

            var e0xx = Vector3.Lerp(e00x, e01x, blendValue.y);
            var e1xx = Vector3.Lerp(e10x, e11x, blendValue.y);

            return Vector3.Lerp(e0xx, e1xx, blendValue.z);
        }

        Vector3 v000;
        Vector3 v001;
        Vector3 v010;
        Vector3 v011;
        Vector3 v100;
        Vector3 v101;
        Vector3 v110;
        Vector3 v111;
    }
}

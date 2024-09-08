using UnityEngine;

namespace CarbideFunction.Wildtile
{
    public static class NormalWarperExtensions
    {
        /// <summary>
        /// Combines the positional warper getting and the warping of the normal into an easily callable method.
        /// </summary>
        public static Vector3 WarpNormal(this NormalWarper warper, Vector3 position, Vector3 normal)
        {
            return warper.GetWarperAtPoint(position).WarpNormal(normal);
        }
    }
}

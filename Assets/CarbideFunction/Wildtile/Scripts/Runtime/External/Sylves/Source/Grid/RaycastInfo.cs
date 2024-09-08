#if UNITY
using UnityEngine;
#endif

namespace CarbideFunction.Wildtile.Sylves
{
    public struct RaycastInfo
    {
        public Cell cell;
        public Vector3 point;
        public float distance;
        public CellDir? cellDir;
    }
}

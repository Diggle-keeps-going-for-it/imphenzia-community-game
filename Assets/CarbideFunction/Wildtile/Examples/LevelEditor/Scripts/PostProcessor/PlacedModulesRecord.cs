using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;

public class PlacedModulesRecord : MonoBehaviour
{
    public class PlacedModule
    {
        public GameObject instance;
        public Slot.SourceVoxels sourceVoxels;
    }
    public PlacedModule[] placedModules = null;
    public Dictionary<int, int[]> voxelIndexToSlotIndices = null;
}

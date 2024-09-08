using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Postprocessing
{

/// <summary>
/// A simplified map that Wildtile passes to postprocessors. Postprocessors can then create Unity game objects and assets based on this map.
/// </summary>
public class PostprocessableMap
{
    public PostprocessableMap(int dimension)
    {
        this.slots = new Slot[dimension];
    }

    public struct SlotHalfLoop
    {
        public Slot targetSlot;
        public Face facingFaceOnTarget;
    }

    /// <summary>
    /// A simplified slot containing the transform for the slot and the prefab that should be placed here.
    ///
    /// The transform is a combination of the slot's position and the module's custom transform (rotation and whether it's flipped)
    /// </summary>
    public class Slot
    {
        public GameObject prefab;
        public ModuleMesh mesh;
        public bool flipIndices;
        public string moduleName;

        public FaceData<SlotHalfLoop> halfLoops;

        public Wildtile.Slot.SourceVoxels sourceVoxels;

        public Vector3 v000;
        public Vector3 v001;
        public Vector3 v010;
        public Vector3 v011;
        public Vector3 v100;
        public Vector3 v101;
        public Vector3 v110;
        public Vector3 v111;

        public NormalWarper normalWarper;
    }

    /// <summary>
    /// An array of the modules.
    ///
    /// Use the <see cref="Slot"/>'s position and other transform data to access each slot's position.
    /// </summary>
    public Slot[] slots;
}

}


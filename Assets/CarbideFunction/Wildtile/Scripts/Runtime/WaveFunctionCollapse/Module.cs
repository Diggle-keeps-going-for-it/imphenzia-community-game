using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// A module is a tile and meta-data associated with the tile, such as how it connects to other tiles and its selection weight.
/// The tile itself is stored as a prefab, and can be accessed through the <see cref="prefab"/> field.
/// </summary>
[Serializable]
public class Module
{
    /// <summary>
    /// A human-readable name that is used to identify modules. This is unique within a tileset.
    /// </summary>
    [SerializeField]
    public string name;

    /// <summary>
    /// A prefab that will be spawned when this module is placed in the world by most PostProcessors. The module and all first level children will be positioned using the warped grid. All deeper children will retain their local transforms, so they will only be warped by their ancestor's transform.
    ///
    /// If null, no object will be spawned for this module.
    /// </summary>
    [SerializeField]
    public GameObject prefab;

    /// <summary>
    /// Extracted mesh data for this module. PostProcessors will warp and weld this mesh to construct the final mesh.
    /// </summary>
    [SerializeField]
    public ModuleMesh mesh;

    /// <summary>
    /// The face indices uniquely identify the tile's face layouts when the tile is not rotated. <see cref="TransformedModule"/> will map the layouts and select the correct default or flipped indices, so you should access the Module's <see ref="FaceLayoutIndex">FaceIndices</see> through the TransformedModule most of the time.
    /// </summary>
    [Serializable]
    public struct FaceIndices
    {
        /// <summary>
        /// Each of the faces in the top face array corresponds to a rotation of the module. <see cref="TransformedModule"/> will select and present the correct face from this array.
        /// </summary>
        public FaceLayoutIndex[] top;

        /// <summary>
        /// Each of the faces in the bottom face array corresponds to a rotation of the module. <see cref="TransformedModule"/> will select and present the correct face from this array.
        /// </summary>
        public FaceLayoutIndex[] bottom;

        /// <summary>
        /// Each of the faces in the side face array corresponds to a face on the side of the module. <see cref="TransformedModule"/> will rotate the array and present all face from this array in the correct order.
        /// </summary>
        public FaceLayoutIndex[] sides;
    }

    [SerializeField]
    public FaceIndices faceIndices;
    [SerializeField]
    public FaceIndices flippedFaceIndices;
}

}

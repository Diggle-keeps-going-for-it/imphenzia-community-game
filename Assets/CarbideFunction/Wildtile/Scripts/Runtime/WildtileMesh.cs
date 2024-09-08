using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// Tag component that signifies that this object contains a mesh that should be extracted to CPU memory and warped and welded by Wildtile.
    ///
    /// On import, the Wildtile tileset importer will destroy this component and sibling mesh renderers and mesh filters.
    /// </summary>
    [DisallowMultipleComponent]
    public class WildtileMesh : MonoBehaviour
    {
        [Tooltip("After meshes have been extracted from this prefab by the Tileset Importer, Wildtile will destroy this component and the sibling MeshRenderer and MeshFilter. If the game object is now empty, the whole object will be destroyed if this field is unchecked.")]
        public bool keepObjectAfterMeshStripping = false;
    }
}

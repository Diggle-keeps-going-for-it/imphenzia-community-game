using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Holds a single tile game object for each marching cube configuration.
/// 
/// This is intended to be used by user-code when creating level editors.
/// </summary>
[CreateAssetMenu(fileName="New Placeholder Tiles", menuName= MenuConstants.topMenuName+"Placeholder Tiles", order=MenuConstants.orderBase + 20)]
public class DualGridVoxelMeshPlaceholderTiles : ScriptableObject
{
    /// <summary>
    /// Return the prefab for the marching cube <paramref name="cubeConfig"/>
    /// </summary>
    public ModuleMesh GetMeshObjectForCubeConfig(int cubeConfig)
    {
        Assert.IsTrue(cubeConfig >= 0x00);
        Assert.IsTrue(cubeConfig <= 0xFF);
        return tiles[cubeConfig];
    }

    [SerializeField]
    private ModuleMesh[] tiles;
    /// <summary>
    /// The name of the tiles property. This is intended to be used to serialize within Unity.
    /// </summary>
    public const string tilesName = nameof(tiles);
}

}

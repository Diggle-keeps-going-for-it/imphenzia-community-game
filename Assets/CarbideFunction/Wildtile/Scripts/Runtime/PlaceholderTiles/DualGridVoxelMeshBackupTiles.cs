using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Holds a single tile module mesh for each marching cube configuration
/// 
/// This is used by the <see cref="GridPlacer"/> when the tile has not been collapsed or if the slot is wild.
/// </summary>
[CreateAssetMenu(fileName="New Backup Tiles", menuName= MenuConstants.topMenuName+"Backup Tiles", order=MenuConstants.orderBase + 20)]
public class DualGridVoxelMeshBackupTiles : ScriptableObject
{
    /// <summary>
    /// Get the module mesh for a marching cube index.
    /// </summary>
    /// <param name="cubeConfig">The marching cube index to get. Should be between 0-255 (inclusive).</param>
    public ModuleMesh GetMeshObjectForCubeConfig(int cubeConfig)
    {
        Assert.IsTrue(cubeConfig >= 0x00);
        Assert.IsTrue(cubeConfig <= 0xFF);
        return tiles[cubeConfig];
    }

    [SerializeField]
    private ModuleMesh[] tiles;

    /// <summary>
    /// Name of the tiles property. Used when serializing in Unity.
    /// </summary>
    public const string tilesName = nameof(tiles);
}

}

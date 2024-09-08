using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// <see cref="Tileset"/> extension methods to create a <see cref="WaveFunctionCollapser"/>.
/// </summary>
public static class TilesetToWaveFunctionCollapser
{
    /// <summary>
    /// Create a <see cref="WaveFunctionCollapser"/> from this <see cref="Tileset"/>. Changing the seed may change the 
    /// </summary>
    /// <param name="tileset">The collapser will use this Tileset's modules when placing modules for a voxel grid.</param>
    /// <param name="seed">If there are random choices when collapsing, changing the seed may change which choice is picked.</param>
    public static WaveFunctionCollapser CreateWaveFunctionCollapser(this Tileset tileset, int seed)
    {
        return new WaveFunctionCollapser(tileset.modules, tileset.horizontalMatchingFaceLayoutIndices, tileset.verticalMatchingFaceLayoutIndices, seed);
    }
}

}

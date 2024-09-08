using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor.MissingTilesParametersGenerators
{

/// <summary>
/// Calculates the minimal set of marching cube configurations that Wildtile can expand into the all required marching cubes. by flipping across X and rotating about Y.
///
/// This does not generate meshes for the tiles, just the layout of corner contents.
///
/// Marching cubes algorithms require 256 tiles - in a cube there are 8 vertices. Each vertex can be filled or empty, so there are 2<sup>8</sup> = 256 required models.
///
/// Wildtile rotates and flips meshes to reuse them across multiple different marching cubes, for example a tile covering a single corner could be rotated to cover any of the four corners.
/// Wildtile's reuse means artists only need to create 53 distinct tiles. The generated list also includes completely empty and completely filled tiles, but these are automatically provided by Wildtile and artists do not need to manually supply them.
/// </summary>
internal static class MinimalTilesThatCanRotateAndFlipToCreateAllTilesGenerator
{
    /// <summary>
    /// This is the root calculation method. Call this to calculate the 55 tile configurations required.
    /// </summary>
    public static List<Tile> GetMinimalTilesThatCanRotateAndFlipToCreateAllTiles()
    {
        var result = new List<Tile>();

        for (var consideredIndex = 0; consideredIndex < 256; ++consideredIndex)
        {
            var consideredTile = Tile.FromYzxTileIndex(consideredIndex);
            if (!result.Any(existingTile => CanBeFlippedOrRotatedToMatch(consideredTile, existingTile)))
            {
                result.Add(consideredTile);
            }
        }

        return result;
    }

    private delegate Tile TileManipulator(Tile tileIndex);
    private static readonly TileManipulator[] rotators = new TileManipulator[]{
        tileIndex => tileIndex,
        tileIndex => RotateAroundYByNinety(tileIndex, 1),
        tileIndex => RotateAroundYByNinety(tileIndex, 2),
        tileIndex => RotateAroundYByNinety(tileIndex, 3),
    };

    // don't need flip across Z because flip across X and rotate 180 are identical
    private static readonly TileManipulator[] flippers = new TileManipulator[]{
        tileIndex => tileIndex,
        tileIndex => FlipAcrossX(tileIndex),
    };

    private static bool CanBeFlippedOrRotatedToMatch(Tile left, Tile right)
    {
        foreach (var rotator in rotators)
        {
            var rotatedLeft = rotator(left);
            foreach (var flipper in flippers)
            {
                var flippedAndRotatedLeft = flipper(rotatedLeft);
                if (flippedAndRotatedLeft.Equals(right))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Tile RotateAroundYByNinety(Tile tile, int numberOfRotations)
    {
        if (numberOfRotations == 0)
        {
            return tile;
        }
        else
        {
            return RotateAroundYByNinety(new Tile{
                lowContents = RotateOneLayerAroundYByNinety(tile.lowContents),
                highContents = RotateOneLayerAroundYByNinety(tile.highContents),
            }, numberOfRotations - 1);
        }
    }

    private static bool[] RotateOneLayerAroundYByNinety(bool[] layerContents)
    {
        return new bool[]{
            layerContents[2], layerContents[0],
            layerContents[3], layerContents[1],
        };
    }

    private static Tile FlipAcrossX(Tile tile)
    {
        return new Tile{
            lowContents = FlipOneLayerAcrossX(tile.lowContents),
            highContents = FlipOneLayerAcrossX(tile.highContents),
        };
    }

    private static bool[] FlipOneLayerAcrossX(bool[] layerContents)
    {
        return new bool[]{
            layerContents[1], layerContents[0],
            layerContents[3], layerContents[2],
        };
    }
}

}

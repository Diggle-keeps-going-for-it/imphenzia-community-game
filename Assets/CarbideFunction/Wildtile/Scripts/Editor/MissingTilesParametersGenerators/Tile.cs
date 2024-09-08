using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor.MissingTilesParametersGenerators
{
    /// <summary>
    /// A single tile layout. Can be converted from int to Tile, then to int. Does not contain or generate any meshes.
    /// </summary>
    internal class Tile
    {
        /// <summary>
        /// Convert an int into a unique tile. Every integer from 0-256 maps to a unique tile.
        /// </summary>
        public static Tile FromYzxTileIndex(int yzxTileIndex)
        {
            var lowContentsIndex = yzxTileIndex & 0xf;
            var highContentsIndex = (yzxTileIndex >> 4) & 0xf;

            return new Tile{
                lowContents = ContentsIndexToContents(lowContentsIndex),
                highContents = ContentsIndexToContents(highContentsIndex),
            };
        }

        private static bool[] ContentsIndexToContents(int contents)
        {
            return new bool[]{
                (contents & 0b0001) != 0,
                (contents & 0b0010) != 0,
                (contents & 0b0100) != 0,
                (contents & 0b1000) != 0,
            };
        }

        // zx lookup
        public bool[] lowContents = new bool[4];
        public bool[] highContents = new bool[4];

        /// <summary>
        /// Turns a tile into the same format that is used by Wildtile to look up tiles in its marching cubes.
        /// </summary>
        public int GetTileZyxIndex()
        {
            var lowContentsContribution = GetContentsContributionAsLow(lowContents);
            var highContentsContribution = GetContentsContributionAsLow(highContents) << 2;
            return lowContentsContribution | highContentsContribution;
        }

        private static int GetContentsContributionAsLow(bool[] contents)
        {
            var aggregateContribution = 0;
            aggregateContribution |= BoolToInt(contents[0]) << 0;
            aggregateContribution |= BoolToInt(contents[1]) << 1;
            aggregateContribution |= BoolToInt(contents[2]) << 4;
            aggregateContribution |= BoolToInt(contents[3]) << 5;
            return aggregateContribution;
        }

        private static int BoolToInt(bool contents)
        {
            return contents ? 1 : 0;
        }

        /// <summary>
        /// Checks if two tiles are immediately equal, without applying any transforms on either tile.
        /// </summary>
        public bool Equals(Tile other)
        {
            return DoContentsMatch(lowContents, other.lowContents)
                && DoContentsMatch(highContents, other.highContents);
        }

        private static bool DoContentsMatch(bool[] leftContents, bool[] rightContents)
        {
            if (leftContents.Length != rightContents.Length)
            {
                return false;
            }

            for (var i = 0; i < leftContents.Length; ++i)
            {
                if (leftContents[i] != rightContents[i])
                {
                    return false;
                }
            }

            return true;
        }

        private string ToBinary(bool value)
        {
            return value ? "1" : "0";
        }

        /// <summary>
        /// Describe this tile uniquely as a string.
        /// </summary>
        public override string ToString()
        {
            return $"{highContents.Select(ToBinary)}-{lowContents.Select(ToBinary)}";
        }
    }
}

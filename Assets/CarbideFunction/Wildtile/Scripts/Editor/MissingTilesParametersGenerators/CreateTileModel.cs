using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.MissingTilesParametersGenerators
{
    /// <summary>
    /// Contains methods that turn <see cref="MissingTilesParametersGenerators.Tile"/>s into Unity <see href="https://docs.unity3d.com/ScriptReference/Mesh.html">Meshes</see>.
    ///
    /// This uses <see cref="CarbideFunction.Wildtile.VoxelMesher"/> to do the meshing, but provides a bridge that hides the generation and use of <see cref="VoxelGrid"/>s.
    /// </summary>
    internal static class CreateTileModel
    {
        /// <summary>
        /// Creates a mesh for a given tile layout. This mesh is not an asset when returned, but is suitable for saving to an asset.
        /// </summary>
        public static Mesh CreateModel(Tile tile)
        {
            var voxelLayout = CreateVoxelLayout(tile);
            var generatedMesh = VoxelMesher.CreateAndGenerateMesh(voxelLayout, Vector3.one);
            generatedMesh.mesh.name = Convert.ToString(tile.GetTileZyxIndex(), 2).PadLeft(8, '0');
            return generatedMesh.mesh;
        }

        private static VoxelGrid CreateVoxelLayout(Tile tile)
        {
            var result = ScriptableObject.CreateInstance<VoxelGrid>();
            result.SetDimensionsAndClearGrid(new Vector3Int(2,2,2));

            ApplyContentsToVoxel(tile.lowContents, 0, result);
            ApplyContentsToVoxel(tile.highContents, 1, result);

            return result;
        }

        private static void ApplyContentsToVoxel(bool[] zxContents, int yBias, VoxelGrid destination)
        {
            for (var i = 0; i < 4; i++)
            {
                var x = i & 1;
                var z = (i & 2) >> 1;
                var voxelPosition = new Vector3Int(x,yBias,z);
                var contents = zxContents[i] ? 1 : 0;

                destination[voxelPosition] = contents;
            }
        }
    }
}

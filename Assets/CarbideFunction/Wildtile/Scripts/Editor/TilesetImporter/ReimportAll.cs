using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class ReimportAll
    {
        public static void Reimport()
        {
            var importerAssets = GetAllImporterAssets();
            foreach (var importer in importerAssets)
            {
                if (importer.destinationTileset != null)
                {
                    TilesetImporter.ImportWithoutFeedback(importer);
                }
                else
                {
                    Debug.Log($"Skipping importer \"{importer.name}\" because it doesn't have a destination tileset");
                }
            }
        }

        private static TilesetImporterAsset[] GetAllImporterAssets()
        {
            var assetGuids = AssetDatabase.FindAssets($"t: {nameof(TilesetImporterAsset)}", new []{"Assets"});
            var loadedAssets = assetGuids.Select(guid => AssetDatabase.LoadAssetAtPath<TilesetImporterAsset>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();
            return loadedAssets;
        }
    }
}

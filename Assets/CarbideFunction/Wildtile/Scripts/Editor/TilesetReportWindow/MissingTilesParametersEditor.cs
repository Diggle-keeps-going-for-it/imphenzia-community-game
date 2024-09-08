using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Editor.MissingTilesParametersGenerators;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Editor for <see cref="MissingTilesParameters"/>. Exposes the default editor and adds an auto populator button.
/// </summary>
[CustomEditor(typeof(MissingTilesParameters))]
internal class MissingTilesParametersEditor : UnityEditor.Editor
{
    private const string undoName = "Generate Missing Tiles Search Parameters contents";

    /// <summary>
    /// Unity Engine called method, do not call manually.
    ///
    /// Draws the GUI and reacts to button presses.
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        if (GUILayout.Button("Generate"))
        {
            var targetAssetPath = GetAssetPath();
            var allAssetsAtTargetPath = GetAllAssetsAtTargetPath(targetAssetPath);
            Undo.RecordObjects(allAssetsAtTargetPath, undoName);
            ClearCurrentSubAssets(allAssetsAtTargetPath);

            var minimalTileIndices = MinimalTilesThatCanRotateAndFlipToCreateAllTilesGenerator.GetMinimalTilesThatCanRotateAndFlipToCreateAllTiles();
            var minimalTilesWithModels = minimalTileIndices.Select(tile => new TileAndMesh{tile=tile, mesh=CreateTileModel.CreateModel(tile)}).ToList();

            SerializeToTarget(minimalTilesWithModels, targetAssetPath);
        }
    }

    private string GetAssetPath()
    {
        return AssetDatabase.GetAssetPath(target);
    }

    private UnityEngine.Object[] GetAllAssetsAtTargetPath(string targetAssetPath)
    {
        return AssetDatabase.LoadAllAssetsAtPath(targetAssetPath);
    }

    private void ClearCurrentSubAssets(UnityEngine.Object[] allAssetObjects)
    {
        foreach (var assetObject in allAssetObjects)
        {
            if (AssetDatabase.IsSubAsset(assetObject))
            {
                AssetDatabase.RemoveObjectFromAsset(assetObject);
            }
        }
    }

    private struct TileAndMesh
    {
        public Tile tile;
        public Mesh mesh;
    }

    private void SerializeToTarget(List<TileAndMesh> tiles, string targetAssetPath)
    {
        var tileList = serializedObject.FindProperty(nameof(MissingTilesParameters.searchConfigurations));
        tileList.arraySize = tiles.Count;

        for (var tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
        {
            var tileElement = tileList.GetArrayElementAtIndex(tileIndex);
            var tile = tiles[tileIndex];
            tileElement.FindPropertyRelative(nameof(SearchConfiguration.marchingCubeConfig)).intValue = tile.tile.GetTileZyxIndex();
            Undo.RegisterCreatedObjectUndo(tile.mesh, undoName);
            AssetDatabase.AddObjectToAsset(tile.mesh, targetAssetPath);
            tileElement.FindPropertyRelative(nameof(SearchConfiguration.representativeModel)).objectReferenceValue = tile.mesh;
        }
        
        serializedObject.ApplyModifiedProperties();
    }
}

}

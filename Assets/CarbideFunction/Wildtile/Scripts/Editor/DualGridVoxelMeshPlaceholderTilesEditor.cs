using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Custom editor for <see cref="DualGridVoxelMeshPlaceholderTiles"/>.
///
/// Provides the ability to generate and populate the placeholder model game objects.
/// </summary>
[CustomEditor(typeof(DualGridVoxelMeshPlaceholderTiles))]
internal class DualGridVoxelMeshPlaceholderTilesEditor : UnityEditor.Editor
{
    [SerializeField]
    private Material placeholderFaceMaterial = null;

    private const float modelScale = 0.9f;

    private const string undoGenerateName = "Generate default placeholder tiles models";
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        if (GUILayout.Button("Generate default models"))
        {
            GenerateAndSaveDefaultModelsToAsset();
        }
    }

    private void GenerateAndSaveDefaultModelsToAsset()
    {
        Undo.RecordObject(target, undoGenerateName);

        var destinationArray = serializedObject.FindProperty(DualGridVoxelMeshPlaceholderTiles.tilesName);
        destinationArray.arraySize = 0x100;

        SaveEmptyMesh(destinationArray, 0x00);
        SaveEmptyMesh(destinationArray, 0xFF);

        for (var cubeConfig = 1; cubeConfig < 0xFF; ++cubeConfig)
        {
            var mesh = DualGridVoxelMeshBackupTilesEditor.GenerateMesh(cubeConfig, placeholderFaceMaterial, null, null);
            SaveMesh(destinationArray, mesh, cubeConfig);
        }

        serializedObject.ApplyModifiedProperties();

        AssetDatabase.SaveAssets();
    }

    private void SaveEmptyMesh(SerializedProperty destinationArray, int cubeConfig)
    {
        var emptyMesh = CreateEmptyMesh();
        SaveMesh(destinationArray, emptyMesh, cubeConfig);
    }

    private ModuleMesh CreateEmptyMesh()
    {
        var mesh = new ModuleMesh();
        mesh.vertices = new Vector3[0];
        mesh.normals = new Vector3[0];
        mesh.uvs = new ModuleMesh.UvChannel[0];
        mesh.triangles = new int[0];
        mesh.subMeshes = new ModuleMesh.SubMesh[0];
        return mesh;
    }

    private void SaveMesh(SerializedProperty destinationArray, ModuleMesh mesh, int cubeConfig)
    {
        var meshProperty = destinationArray.GetArrayElementAtIndex(cubeConfig);
        ModuleMeshSerializer.SaveMeshToProperty(meshProperty, mesh);
    }

    private static bool ShouldCreateFilledPrefabForPlaceholder(int cubeConfig)
    {
        return cubeConfig != 0 && cubeConfig != 0xFF;
    }
}

}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using CarbideFunction.Wildtile;

using ModuleConnectivityData = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ModuleConnectivityData;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class manages the UI frontend for the <see cref="TilesetImporter"/>. It adds buttons and functionality to import basic models into a <see cref="Tileset"/> asset.
/// </summary>
[CustomEditor(typeof(TilesetImporterAsset))]
internal class TilesetImporterAssetEditor : UnityEditor.Editor
{
    private const string undoOperationName = "Import model to marching cubes";
    private bool showStepsButtons = false;
    public override void OnInspectorGUI()
    {
        DrawProperties();
        DrawButtons();
    }

    private void DrawProperties()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TilesetImporterAsset.sourceModel)));

        DrawUneditableList(serializedObject.FindProperty(nameof(TilesetImporterAsset.userEditableImportedModelPrefabVariants)), userEditableGuiContent);

        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TilesetImporterAsset.extraPrefabs)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TilesetImporterAsset.importerSettings)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TilesetImporterAsset.postprocessorCreator)));
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(TilesetImporterAsset.destinationTileset)));

        serializedObject.ApplyModifiedProperties();
    }

    // copy the tooltip over because the [TooltipAttribute] will add the tooltip to the array elements
    // and this GUIContent will add the tooltip to the array header.
    private static readonly GUIContent userEditableGuiContent = new GUIContent(
        "Imported Model Prefabs",
        TilesetImporterAsset.userEditablePrefabsTooltip
    );

    private void DrawUneditableList(SerializedProperty listRoot, GUIContent guiContent)
    {
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(listRoot, guiContent);
        }
    }

    private void DrawButtons()
    {
        var importerTarget = (TilesetImporterAsset)target;
        using (new EditorGUI.DisabledScope(importerTarget.destinationTileset == null))
        {
            if (GUILayout.Button("Process models"))
            {
                TilesetImporter.Import(importerTarget);
                serializedObject.Update();
            }

            showStepsButtons = EditorGUILayout.BeginFoldoutHeaderGroup(showStepsButtons, "Individual Import Steps");
            if (showStepsButtons)
            {
                if (GUILayout.Button("Preprocess models to prefabs"))
                {
                    TilesetImporter.PreprocessModel(importerTarget);
                    serializedObject.Update();
                }

                if (GUILayout.Button("Generate Tileset"))
                {
                    TilesetImporter.GenerateTileset(importerTarget);
                    serializedObject.Update();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }
}

}

using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.SceneManagement;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class shows information about <see cref="GridPlacer"/> components in the inspector window.
/// It shows the default inspector fields and adds buttons for interacting with the constructed model grid - one button triggers a force-refresh of the temporary grid
/// and the other button bakes out a permanent instance of the grid's modules that the user can then manipulate afterwards.
///
/// The invocation of the WFC algorithm starts in <see cref="GridPlacer"/>.
///
/// This does not affect the onscreen grid editor - that is implemented in <see cref="TileMapEditorTool"/>.
/// </summary>
[CustomEditor(typeof(GridPlacer))]
internal class GridPlacerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        if (GUILayout.Button("Force Map Rebuild"))
        {
            Placer.GenerateOverTime();
        }

        EditorGUI.BeginDisabledGroup(IsTargetGenerating());
        try
        {
            if (GUILayout.Button("Duplicate to Permanent Instance"))
            {
                DuplicateMap();
            }
        }
        finally
        {
            EditorGUI.EndDisabledGroup();
        }
    }

    private const string bakeMapUndoName = "Bake map";

    private static GameObject SpawnEditorLinkedPrefab(GameObject prefab, Transform parent)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        instance.transform.parent = parent;
        Undo.RegisterCreatedObjectUndo(instance, bakeMapUndoName);
        return instance;
    }

    private void DuplicateMap()
    {
        // needs to save the mesh asset
        var child = Placer.transform.GetChild(0);
        var duplicatedModel = GameObject.Instantiate(child.gameObject);

        MoveToCurrentSceneAndParentIfNeeded(duplicatedModel, child.gameObject.scene);

        duplicatedModel.transform.position = child.position;
        duplicatedModel.transform.rotation = child.rotation;
        duplicatedModel.transform.localScale = child.localScale;

        duplicatedModel.hideFlags = HideFlags.None;

        var clonedTransientObjects = Placer.Tileset.Postprocessor.CloneAndReferenceTransientObjects(duplicatedModel.gameObject);
        if (clonedTransientObjects != null && clonedTransientObjects.Length > 0)
        {
            SaveAsPermanentAssets(clonedTransientObjects);
        }
    }

    private void MoveToCurrentSceneAndParentIfNeeded(GameObject duplicatedWorld, Scene placerScene)
    {
        SceneManager.MoveGameObjectToScene(duplicatedWorld, placerScene);

        var stage = StageUtility.GetStage(Placer.gameObject);

        if (stage is UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            var rootObject = placerScene.GetRootGameObjects().FirstOrDefault(rootObject => rootObject.hideFlags == HideFlags.None);
            Assert.IsNotNull(rootObject);
            duplicatedWorld.transform.SetParent(rootObject.transform);
        }
    }

    private bool IsTargetGenerating()
    {
        return Placer.IsGenerating;
    }

    private GridPlacer Placer => (GridPlacer)target;

    private void SaveAsPermanentAssets(UnityEngine.Object[] referencedTransientObjects)
    {
        var assetPath = GetRootRelativeAssetPath();
        var dummyObject = ScriptableObject.CreateInstance<DuplicatedMapRootAsset>();
        AssetDatabase.CreateAsset(dummyObject, assetPath);

        foreach (var transientObject in referencedTransientObjects)
        {
            AssetDatabase.AddObjectToAsset(transientObject, assetPath);
        }

        AssetDatabase.SaveAssets();
    }

    private string GetRootRelativeAssetPath()
    {
        var containingAssetPath = GetSceneOrPrefabPathWithoutExtension();
        var folderPath = EnsureFolderExists(Path.GetDirectoryName(containingAssetPath), Path.GetFileName(containingAssetPath));
        var idealAssetPath = folderPath + "/" + Placer.gameObject.name + ".asset";
        var uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(idealAssetPath);
        return uniqueAssetPath;
    }

    private string GetSceneOrPrefabPathWithoutExtension()
    {
        var stage = StageUtility.GetStage(Placer.gameObject);

        if (stage is UnityEditor.SceneManagement.PrefabStage prefabStage)
        {
            var path = prefabStage.assetPath;
            return path.Remove(path.Length - ".prefab".Length);
        }
        else
        {
            var path = Placer.gameObject.scene.path;
            return path.Remove(path.Length - ".unity".Length);
        }
    }

    private string EnsureFolderExists(string folderRootPath, string newFolderName)
    {
        var combinedFolderPath = folderRootPath + "/" + newFolderName;

        if (!AssetDatabase.IsValidFolder(combinedFolderPath))
        {
            var folderGuid = AssetDatabase.CreateFolder(folderRootPath, newFolderName);
            return AssetDatabase.GUIDToAssetPath(folderGuid);
        }
        else
        {
            return combinedFolderPath;
        }
    }
}

}

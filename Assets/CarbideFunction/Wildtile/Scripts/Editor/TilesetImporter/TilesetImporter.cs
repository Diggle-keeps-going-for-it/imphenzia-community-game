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

internal static class TilesetImporter
{
    private const string undoOperationName = "Import model to marching cubes";

    private const string wildtileAssetsPath = "Wildtile";
    private static readonly string rawModelPrefabsPath = "RawModels";
    private static readonly string fullyImportedModulePrefabsPath = "FullyImported";

    private const string progressBarTitle = "Voxel Tileset Importer";
    internal static void Import(TilesetImporterAsset importerTarget)
    {
        var importDetails = new ImportDetails();
        try
        {
            PreprocessModelInternal(importerTarget, progressBarInfo => EditorUtility.DisplayProgressBar(progressBarTitle, progressBarInfo, 0f));
            GenerateTilesetInternal(importerTarget, importDetails, progressBarInfo => EditorUtility.DisplayProgressBar(progressBarTitle, progressBarInfo, 0f));
        }
        catch (Exception e)
        {
            importDetails.exceptionMessage = e.Message;

            if (UserSettings.instance.rethrowImportExceptions)
            {
                throw;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            ImportReport.TilesetReportWindow.ShowReport(importerTarget, importDetails);
        }
    }

    internal static void ImportWithoutFeedback(TilesetImporterAsset importerTarget)
    {
        var importDetails = new ImportDetails();
        PreprocessModelInternal(importerTarget, progressBarInfo => {});
        GenerateTilesetInternal(importerTarget, importDetails, progressBarInfo => {});
    }

    internal static void PreprocessModel(TilesetImporterAsset importerTarget)
    {
        try
        {
            PreprocessModelInternal(importerTarget, progressBarInfo => EditorUtility.DisplayProgressBar(progressBarTitle, progressBarInfo, 0f));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    internal static void GenerateTileset(TilesetImporterAsset importerTarget)
    {
        var importDetails = new ImportDetails();
        try
        {
            GenerateTilesetInternal(importerTarget, importDetails, progressBarInfo => EditorUtility.DisplayProgressBar(progressBarTitle, progressBarInfo, 0f));
        }
        catch (Exception e)
        {
            importDetails.exceptionMessage = e.Message;
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            ImportReport.TilesetReportWindow.ShowReport(importerTarget, importDetails);
        }
    }

    private static void PreprocessModelInternal(TilesetImporterAsset importerTarget, Action<String> reportProgress)
    {
        reportProgress("Setting up for preprocessing models");
        Undo.RecordObject(importerTarget, undoOperationName);

        var previousPreprocessedModelPrefabPaths = CacheExistingPreprocessedModelPrefabs(importerTarget);
        var previousUserEditablePrefabVariantPaths = CacheExistingPrefabs(importerTarget.userEditableImportedModelPrefabVariants);

        var serializedObject = new SerializedObject(importerTarget);
        if (importerTarget.sourceModel != null)
        {
            reportProgress("Creating module asset folders");
            var prefabFolderPath = EnsureModelPrefabFolderExistsAndGetPath(importerTarget.destinationTileset);
            var preprocessedModelPrefabsFolderPath = EnsurePreprocessedModelPrefabFolderExistsAndGetPath(prefabFolderPath);
            reportProgress("Creating user editable base prefabs");
            var modelPrefabs = CreatePrefabsForSubMeshes(importerTarget, preprocessedModelPrefabsFolderPath);

            reportProgress("Loading or creating user editable prefab variants");
            var userEditablePrefabVariants = CreateOrGetExistingPrefabVariants(modelPrefabs, previousUserEditablePrefabVariantPaths, prefabFolderPath);

            reportProgress("Deleting now-unused user editable prefab variants");
            var newlySavedUserEditableVariantPaths = CacheExistingPrefabs(userEditablePrefabVariants);

            reportProgress("Saving prefab references to tileset importer asset");
            SaveModelPrefabsToSerializedObject(serializedObject, modelPrefabs);
            SaveUserEditablePrefabVariantsToSerializedObject(serializedObject, userEditablePrefabVariants);
        }
        else
        {
            // no model to import, destroy any existing model-imported prefabs

            reportProgress("Clearing prefab references in tileset importer asset");
            var emptyGameObjectArray = new List<GameObject>();
            SaveModelPrefabsToSerializedObject(serializedObject, emptyGameObjectArray);
            SaveUserEditablePrefabVariantsToSerializedObject(serializedObject, emptyGameObjectArray);
        }

        serializedObject.ApplyModifiedProperties();

        reportProgress("Destroying old imported model prefabs");
        var postImportPreprocessedModelPrefabPaths = CacheExistingPreprocessedModelPrefabs(importerTarget);
        var removedPreprocessedModelPrefabPaths = previousPreprocessedModelPrefabPaths.Where(path => !postImportPreprocessedModelPrefabPaths.Contains(path));
        var postImportUserEditablePrefabVariantPaths = CacheExistingPrefabs(importerTarget.userEditableImportedModelPrefabVariants);
        var removedUserEditablePrefabVariantPaths = previousUserEditablePrefabVariantPaths.Where(path => !postImportUserEditablePrefabVariantPaths.Contains(path));
        DeletePrefabAssets(removedUserEditablePrefabVariantPaths.Concat(removedPreprocessedModelPrefabPaths).ToArray());
    }

    private static void SaveModelPrefabsToSerializedObject(SerializedObject serializedObject, List<GameObject> prefabs)
    {
        SaveGameObjectListToSerializedProperty(serializedObject.FindProperty(nameof(TilesetImporterAsset.rawImportedModelPrefabs)), prefabs);
    }

    private static void SaveUserEditablePrefabVariantsToSerializedObject(SerializedObject serializedObject, List<GameObject> prefabs)
    {
        SaveGameObjectListToSerializedProperty(serializedObject.FindProperty(nameof(TilesetImporterAsset.userEditableImportedModelPrefabVariants)), prefabs);
    }

    private static void SaveGameObjectListToSerializedProperty(SerializedProperty serializedProperty, List<GameObject> prefabs)
    {
        Assert.IsTrue(serializedProperty.isArray);
        Assert.AreEqual(serializedProperty.arrayElementType, $"PPtr<${typeof(GameObject).Name}>");
        serializedProperty.arraySize = prefabs.Count;
        for (var i = 0; i < prefabs.Count; ++i)
        {
            var arrayElement = serializedProperty.GetArrayElementAtIndex(i);
            arrayElement.objectReferenceValue = prefabs[i];
        }
    }

    private static void DeletePrefabAssets(string[] prefabPathsToDelete)
    {
        var failedToDeletePaths = new List<string>();
        if (!AssetDatabase.DeleteAssets(prefabPathsToDelete, failedToDeletePaths))
        {
            Debug.Log($"Some old imported model prefabs were not deleted:\n{String.Join("\n", failedToDeletePaths)}");
            EditorUtility.DisplayDialog("Failed to Delete", "Some old imported model prefabs were not deleted. Check the logs for more info", "OK");
        }
    }

    private static List<GameObject> CreateOrGetExistingPrefabVariants(List<GameObject> preprocessedPrefabs, List<string> existingPrefabPaths, string userEditablePrefabFolderPath)
    {
        var userEditablePrefabs = new List<GameObject>(preprocessedPrefabs.Count);
        foreach (var prefab in preprocessedPrefabs)
        {
            var userEditableAssetPath = ConstructUserEditableAssetPathForPrefab(userEditablePrefabFolderPath, prefab.name);
            var existingPrefabPathIndex = existingPrefabPaths.FindIndex(assetPath => assetPath == userEditableAssetPath);

            if (existingPrefabPathIndex != -1)
            {
                var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(existingPrefabPaths[existingPrefabPathIndex]);
                userEditablePrefabs.Add(loadedPrefab);
            }
            if (!existingPrefabPaths.Contains(userEditableAssetPath))
            {
                // saving a game object that is already a prefab will save it as a prefab variant
                var prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    Assert.IsTrue(PrefabUtility.IsAnyPrefabInstanceRoot(prefabInstance), $"GameObject {prefabInstance.name} should be a prefab so that SaveAsPrefabAsset saves a variant");
                    var newPrefabVariant = PrefabUtility.SaveAsPrefabAsset(prefabInstance, userEditableAssetPath);
                    userEditablePrefabs.Add(newPrefabVariant);
                }
                finally
                {
                    GameObject.DestroyImmediate(prefabInstance);
                }
            }
        }

        return userEditablePrefabs;
    }

    private static string ConstructUserEditableAssetPathForPrefab(string prefabFolderPath, string prefabName)
    {
        return Path.Join(prefabFolderPath, prefabName + ".prefab");
    }

    private static void GenerateTilesetInternal(TilesetImporterAsset importerTarget, ImportDetails importDetails, Action<String> reportProgress)
    {
        Assert.IsNotNull(importerTarget.destinationTileset, $"No destination tileset for importer {importerTarget.name}");

        Undo.RecordObject(importerTarget.destinationTileset, undoOperationName);
        reportProgress("Setting up for importing models");

        // cache and later delete prefabs to reduce the amount of source control churn when you iteratively
        // create and update new modules and reimport them.
        var previousPrefabPaths = CacheExistingModulePrefabs(importerTarget.destinationTileset);
        ClearTilesetModules(importerTarget.destinationTileset);
        var rootPrefabFolderPath = EnsureModelPrefabFolderExistsAndGetPath(importerTarget.destinationTileset);
        var prefabFolderPath = EnsureTilesetPrefabFolderExistsAndGetPath(rootPrefabFolderPath);
        reportProgress("Cloning prefabs for tileset");
        var prefabsAndMeshes = CreatePermanentPrefabsForTilesetAndExtractModuleMesh(importerTarget, prefabFolderPath);
        reportProgress("Caching connectivity");
        var objectsAndConnections = CacheConnectivityFromModules(prefabsAndMeshes, importerTarget.importerSettings.materialImportSettings);
        reportProgress("Creating empty modules");
        CreateAndAddEmptyPrefab(objectsAndConnections, prefabFolderPath);
        CreateAndAddFilledPrefab(objectsAndConnections, prefabFolderPath);
        reportProgress("Calculating and saving adjacency constraints");

        AddTrianglesOnModuleFacesToImportDetails(objectsAndConnections, importDetails);
        AddSuperInsideOutsideCornersToImportDetails(objectsAndConnections, importDetails);
        AddOutOfBoundsModulesToImportDetails(objectsAndConnections, importDetails);

        HashCollisionReporter hashCollisionReporter = (FaceLayoutHash collidingHash, FaceLayoutHash newOppositeHash, ModuleFaceIdentifier newModule, FaceLayoutHash oldOppositeHash, ModuleFaceIdentifier oldModule) => 
        {
            importDetails.hashCollisions.Add(new ImportDetails.HashCollision{
                collidingHash = collidingHash,
                oppositeHash0 = oldOppositeHash,
                moduleFace0 = oldModule,
                oppositeHash1 = newOppositeHash,
                moduleFace1 = newModule,
            });
        };
        SavePrefabsAndAdjacencyConstraintsToModules(importerTarget.destinationTileset, objectsAndConnections, importerTarget.importerSettings.positionHashResolution, importerTarget.importerSettings.normalHashResolution, hashCollisionReporter);
        SaveTileDimensionsToDestination(importerTarget);
        ClearOldPostprocessorsFromDestination(importerTarget.destinationTileset);
        CreateAndSavePostprocessorInDestination(importerTarget);

        DeleteOldPrefabsThatAreNotInNewPrefabs(importerTarget.destinationTileset, previousPrefabPaths);

        // make sure the assets are saved, if we don't then a reimport will clear some of the updates we've made
        AssetDatabase.SaveAssets();

        importerTarget.NotifyChanged();
    }

    private struct PermanentPrefabAndModuleMesh
    {
        public GameObject prefab;
        public ModuleMesh mesh;
        public string name;
    }

    private static List<PermanentPrefabAndModuleMesh> CreatePermanentPrefabsForTilesetAndExtractModuleMesh
    (
        TilesetImporterAsset importerTarget,
        string prefabFolderPath
    )
    {
        var permanentPrefabsAndMeshes = new List<PermanentPrefabAndModuleMesh>();

        var inverseTileDimensions = CarbideFunction.Wildtile.Sylves.VectorUtils.Divide(Vector3.one, importerTarget.importerSettings.TileDimensions);

        var errorMessages = new List<string>();

        foreach (var userEditableModelPrefab in importerTarget.userEditableImportedModelPrefabVariants)
        {
            ImportPrefabAsModule(userEditableModelPrefab, importerTarget.importerSettings.TileDimensions, inverseTileDimensions, prefabFolderPath, permanentPrefabsAndMeshes, errorMessages);
        }

        foreach (var prefab in importerTarget.extraPrefabs)
        {
            ImportPrefabAsModule(prefab, importerTarget.importerSettings.TileDimensions, inverseTileDimensions, prefabFolderPath, permanentPrefabsAndMeshes, errorMessages);
        }

        if (errorMessages.Count != 0)
        {
            Debug.LogError("Unable to generate prefabs for module meshes");
            foreach (var message in errorMessages)
            {
                Debug.LogError(message);
            }
            throw new Exception("Unable to generate prefabs for module meshes. Check console for more information");
        }

        return permanentPrefabsAndMeshes;
    }

    private static void ImportPrefabAsModule
    (
        GameObject prefab,
        Vector3 tileDimensions,
        Vector3 inverseTileDimensions,
        string prefabFolderPath,
        List<PermanentPrefabAndModuleMesh> permanentPrefabsAndMeshes,
        List<string> errorMessages
    )
    {
        var uniqueName = CreateNonCollidingName(prefab, permanentPrefabsAndMeshes.Select(prevPrefab => prevPrefab.name));
        var prefabInstance = GameObject.Instantiate(prefab);
        try
        {
            prefabInstance.name = uniqueName;
            var mesh = ModuleMeshExtractor.ExtractModelAndDeleteModelObjects(prefabInstance, tileDimensions, inverseTileDimensions, out var errorMessage);
            if (mesh == null)
            {
                // failed to validate/import
                errorMessages.Add(errorMessage);
            }
            else
            {
                // mesh successfully imported
                var savedPrefab = SaveMaybeZombieGameObjectAsPrefab(prefabInstance, Path.Join(prefabFolderPath, uniqueName + ".prefab"));
                if (savedPrefab != null)
                {
                    Assert.AreEqual(savedPrefab.name, uniqueName);
                }

                permanentPrefabsAndMeshes.Add(new PermanentPrefabAndModuleMesh{
                    prefab = savedPrefab,
                    mesh = mesh,
                    name = uniqueName,
                });
            }
        }
        finally
        {
            GameObject.DestroyImmediate(prefabInstance);
        }
    }

    private static GameObject SaveMaybeZombieGameObjectAsPrefab(GameObject maybeZombieGameObject, string path)
    {
        // if maybeZombieGameObject is destroyed, it has a zombie reference to the object
        // fully clear the zombie reference to a null reference to prevent this infecting anything else
        if (maybeZombieGameObject == null)
        {
            return null;
        }

        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(maybeZombieGameObject, path);
        return savedPrefab;
    }

    private static string CreateNonCollidingName(GameObject sourcePrefab, IEnumerable<string> previousPrefabNames)
    {
        var prefabName = sourcePrefab.name;
        var suffixIndex = 0;
        var testingName = prefabName;
        while (previousPrefabNames.Contains(testingName))
        {
            suffixIndex++;
            testingName = ConstructIndexedPrefabName(prefabName, suffixIndex);
        }
        return testingName;
    }

    private static string ConstructIndexedPrefabName(string prefabRootName, int index)
    {
        return $"{prefabRootName}_{index}";
    }

    private static List<string> CacheExistingPreprocessedModelPrefabs(TilesetImporterAsset importerAsset)
    {
        return CacheExistingPrefabs(importerAsset.rawImportedModelPrefabs);
    }

    private static List<string> CacheExistingPrefabs(List<GameObject> prefabs)
    {
        var result = new List<string>();
        if (prefabs != null)
        {
            foreach (var module in prefabs)
            {
                if (module != null)
                {
                    var prefabPath = AssetDatabase.GetAssetPath(module);
                    if (!String.IsNullOrEmpty(prefabPath))
                    {
                        result.Add(StandardizeFileSeparators(prefabPath));
                    }
                }
            }
        }
        return result;
    }

    private static string StandardizeFileSeparators(string wildFilePath)
    {
        return wildFilePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }

    private static List<string> CacheExistingModulePrefabs(Tileset destinationTileset)
    {
        var result = new List<string>();
        if (destinationTileset.modules != null)
        {
            foreach (var module in destinationTileset.modules)
            {
                if (module?.prefab != null)
                {
                    Undo.RecordObject(module.prefab, undoOperationName);

                    var prefabPath = AssetDatabase.GetAssetPath(module.prefab);
                    if (!String.IsNullOrEmpty(prefabPath))
                    {
                        result.Add(prefabPath);
                    }
                }
            }
        }
        return result;
    }

    private static void DeleteOldPrefabsThatAreNotInNewPrefabs(Tileset destinationTileset, List<string> prefabPaths)
    {
        foreach (var oldPrefabPath in prefabPaths)
        {
            if (destinationTileset.modules.All(module => AssetDatabase.GetAssetPath(module.prefab) != oldPrefabPath))
            {
                AssetDatabase.DeleteAsset(oldPrefabPath);
            }
        }
    }

    private static void ClearTilesetModules(Tileset destinationTileset)
    {
        destinationTileset.modules = new List<Module>();
        destinationTileset.ResetCubeLookup();
    }

    private static List<GameObject> CreatePrefabsForSubMeshes(TilesetImporterAsset importerTarget, string prefabFolderPath)
    {
        var sourceMeshes = new List<GameObject>();

        // what submeshes are in the mesh asset?
        var modelPrefab = importerTarget.sourceModel;
        for (var childIndex = 0; childIndex < modelPrefab.transform.childCount; ++childIndex)
        {
            var child = modelPrefab.transform.GetChild(childIndex);
            var childMeshFilter = child.GetComponent<MeshFilter>();
            if (childMeshFilter != null)
            {
                sourceMeshes.Add(child.gameObject);
            }
        }

        var existingNames = new List<string>();

        return sourceMeshes.Select(sourceMesh => CreatePrefabWithMesh(sourceMesh, prefabFolderPath, existingNames)).ToList();
    }

    private static string EnsureModelPrefabFolderExistsAndGetPath(Tileset destinationTileset)
    {
        var destinationPath = AssetDatabase.GetAssetPath(destinationTileset);
        var destinationFolderName = Path.GetFileNameWithoutExtension(destinationPath);
        var destinationRootFolder = Path.GetDirectoryName(destinationPath);

        var prefabFolderPath = Folder.EnsureFolderExists(destinationRootFolder, destinationFolderName);

        return prefabFolderPath;
    }

    private static string EnsureInternalPrefabsFolderExistsAndGetPath(string tilesetRootFolder)
    {
        return Folder.EnsureFolderExists(tilesetRootFolder, wildtileAssetsPath);
    }

    private static string EnsurePreprocessedModelPrefabFolderExistsAndGetPath(string tilesetRootFolder)
    {
        var internalPrefabsFolder = EnsureInternalPrefabsFolderExistsAndGetPath(tilesetRootFolder);
        return Folder.EnsureFolderExists(internalPrefabsFolder, rawModelPrefabsPath);
    }

    private static string EnsureTilesetPrefabFolderExistsAndGetPath(string tilesetRootFolder)
    {
        var internalPrefabsFolder = EnsureInternalPrefabsFolderExistsAndGetPath(tilesetRootFolder);
        return Folder.EnsureFolderExists(internalPrefabsFolder, fullyImportedModulePrefabsPath);
    }

    private static GameObject CreatePrefabWithMesh(GameObject sourceMeshGameObject, string prefabFolderPath, List<string> existingPrefabNames)
    {
        var prefabName = sourceMeshGameObject.name;
        var uniqueName = CreateNonCollidingName(sourceMeshGameObject, existingPrefabNames);
        existingPrefabNames.Add(uniqueName);

        var prefabRoot = new GameObject(uniqueName);
        // Unity does not save the transform for root objects in prefabs.
        // The modelHolder object saves the transform from the source mesh object
        // which is normally introduced when exporting models from modelling software.
        var modelHolder = new GameObject("Model holder");
        modelHolder.transform.SetParent(prefabRoot.transform);
        modelHolder.transform.localPosition = Vector3.zero;
        modelHolder.transform.localRotation = sourceMeshGameObject.transform.rotation;
        modelHolder.transform.localScale = sourceMeshGameObject.transform.localScale;

        var meshFilter = modelHolder.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceMeshGameObject.GetComponent<MeshFilter>().sharedMesh;

        var meshRenderer = modelHolder.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = sourceMeshGameObject.GetComponent<MeshRenderer>().sharedMaterials;

        modelHolder.AddComponent<WildtileMesh>();

        var prefabPath = Path.Join(prefabFolderPath, uniqueName + ".prefab");
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        UnityEngine.Object.DestroyImmediate(prefabRoot);

        var loadedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        return loadedPrefab;
    }

    private class ModuleAndConnectivity
    {
        public string name;
        public GameObject prefabAsset;
        public ModuleMesh mesh;
        public ModuleConnectivityData connectivity;
    }

    private static List<ModuleAndConnectivity> CacheConnectivityFromModules(List<PermanentPrefabAndModuleMesh> prefabsAndMeshes, List<ImporterSettings.MaterialImportSettings> materialImportSettings)
    {
        return prefabsAndMeshes.Select(prefabAndMesh => {
            return new ModuleAndConnectivity{
                name = prefabAndMesh.name,
                prefabAsset = prefabAndMesh.prefab,
                mesh = prefabAndMesh.mesh,
                connectivity = ModelTileConnectivityCalculator.GetConnectivityFromMesh(prefabAndMesh.mesh, materialImportSettings, prefabAndMesh.name)
            };
        }).ToList();
    }

    private static void CreateAndAddEmptyPrefab(List<ModuleAndConnectivity> prefabsAndConnections, string prefabFolderPath)
    {
        CreateAndAddModellessPrefab(prefabsAndConnections, prefabFolderPath, "Empty", connectivity => {});
    }

    private static void CreateAndAddFilledPrefab(List<ModuleAndConnectivity> prefabsAndConnections, string prefabFolderPath)
    {
        CreateAndAddModellessPrefab(prefabsAndConnections, prefabFolderPath, "Filled", connectivity =>
        {
            for (var i = 0; i < connectivity.moduleVertexContents.Length; ++i)
            {
                connectivity.moduleVertexContents[i] = 1;
            }
        });
    }

    private static void CreateAndAddModellessPrefab(List<ModuleAndConnectivity> prefabsAndConnections, string prefabFolderPath, string name, Action<ModuleConnectivityData> populateConnectivity)
    {
        var connectivity = new ModuleConnectivityData();
        populateConnectivity(connectivity);
        prefabsAndConnections.Add(new ModuleAndConnectivity{
            name = name,
            prefabAsset = null,
            mesh = ModuleMesh.CreateEmpty(),
            connectivity = connectivity
        });
    }

    public class FaceIdentifierAndOppositeHash
    {
        public ModuleFaceIdentifier faceIdentifier;
        public FaceLayoutHash oppositeHash;
    }

    private static DeepHashCollisionReporter CreateHashCollisionReporterForHashMappings(Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> hashMappings, HashCollisionReporter hashCollisionReporter)
    {
        return (FaceLayoutHash collidingHash, FaceLayoutHash newOppositeHash, ModuleFaceIdentifier newOppositeFace) =>
        {
            var existingOppositeFace = hashMappings[collidingHash];
            hashCollisionReporter(collidingHash, newOppositeHash, newOppositeFace, existingOppositeFace.oppositeHash, existingOppositeFace.faceIdentifier);
        };
    }

    private static void SavePrefabsAndAdjacencyConstraintsToModules(Tileset destinationTileset, List<ModuleAndConnectivity> prefabsAndConnections, int positionHashResolution, int normalHashResolution, HashCollisionReporter hashCollisionReporter)
    {
        var serializedTileset = new SerializedObject(destinationTileset);
        var serializedModules = serializedTileset.FindProperty(nameof(Tileset.modules));
        var serializedMarchingCubeLookup = serializedTileset.FindProperty(Tileset.marchingCubeLookupName);
        var serializedHorizontalFaceIndices = serializedTileset.FindProperty(nameof(Tileset.horizontalMatchingFaceLayoutIndices));
        var serializedVerticalFaceIndices = serializedTileset.FindProperty(nameof(Tileset.verticalMatchingFaceLayoutIndices));

        var horizontalMatchingFaceHashes = new Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash>();
        var verticalMatchingFaceHashes = new Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash>();

        DeepHashCollisionReporter horizontalDeepHashCollisionReporter = CreateHashCollisionReporterForHashMappings(horizontalMatchingFaceHashes, hashCollisionReporter);
        DeepHashCollisionReporter verticalDeepHashCollisionReporter = CreateHashCollisionReporterForHashMappings(verticalMatchingFaceHashes, hashCollisionReporter);

        var modulesWithHashes = new List<ImportedModule>(prefabsAndConnections.Count());

        for (var prefabIndex = 0; prefabIndex < prefabsAndConnections.Count(); ++prefabIndex)
        {
            var prefabAndFaces = prefabsAndConnections.ElementAt(prefabIndex);
            ImportInfoLog.Log(() => $"Hashing faces for prefab \"{prefabAndFaces.prefabAsset?.name}\", module \"{prefabAndFaces.name}\"");

            var halfFilledFaceIdentifier = new ModuleFaceIdentifier{
                module = $"{prefabAndFaces.name} ({prefabIndex})",
                prefabIndex = prefabIndex,
            };
            var hashedFaces = HashFacesAndEnsureMatchingFaceInMap(prefabAndFaces.connectivity, positionHashResolution, normalHashResolution, horizontalMatchingFaceHashes, verticalMatchingFaceHashes, horizontalDeepHashCollisionReporter, verticalDeepHashCollisionReporter, halfFilledFaceIdentifier.WithFlipped(false));
            var flippedHashedFaces = HashFlippedFacesAndEnsureMatchingFaceInMap(prefabAndFaces.connectivity, positionHashResolution, normalHashResolution, horizontalMatchingFaceHashes, verticalMatchingFaceHashes, horizontalDeepHashCollisionReporter, verticalDeepHashCollisionReporter, halfFilledFaceIdentifier.WithFlipped(true));

            modulesWithHashes.Add(new ImportedModule{
                name = prefabAndFaces.name,
                prefab = prefabAndFaces.prefabAsset,
                mesh = prefabAndFaces.mesh,
                faceHashes = hashedFaces,
                flippedFaceHashes = flippedHashedFaces,
            });

            var moduleContents = prefabAndFaces.connectivity.moduleVertexContents;
            // Do not do full import step for final 2 - they are always the filled and empty modules.
            // Only generate the yaw 0 non mirrored variants for them
            if (prefabIndex < prefabsAndConnections.Count() - 2)
            {
                WriteTransformedModulesToProperties(serializedMarchingCubeLookup, moduleContents, prefabIndex, GetModuleSelectionWeight(prefabAndFaces));
            }
            else
            {
                var marchingCubeIndex = GetMarchingCubeIndexForContents(moduleContents);
                WriteBlockModuleToProperties(serializedMarchingCubeLookup, marchingCubeIndex, prefabIndex);
            }
        }
        
        var hashToIndexMapper = CreateHashToIndexMapper(horizontalMatchingFaceHashes, verticalMatchingFaceHashes);

        WriteFaceIndicesToSerializedProperty(hashToIndexMapper.horizontalMatchingFaces, serializedHorizontalFaceIndices);
        WriteFaceIndicesToSerializedProperty(hashToIndexMapper.verticalMatchingFaces, serializedVerticalFaceIndices);

        var indexifiedModules = MapModuleFaceHashesToFaceIndices(modulesWithHashes, hashToIndexMapper.mapper).ToList();
        WriteModulesToProperties(serializedModules, indexifiedModules);

        ModulesSupportingFaceLayoutCounter.CalculateAndSaveCubeConfigurationStartingSupportedFaceCounts(serializedMarchingCubeLookup, hashToIndexMapper.horizontalMatchingFaces.Length, hashToIndexMapper.verticalMatchingFaces.Length, indexifiedModules);

        serializedTileset.ApplyModifiedProperties();
    }

    private static IEnumerable<Module> MapModuleFaceHashesToFaceIndices(IEnumerable<ImportedModule> importedModules, FaceLayoutHashToIndexMapper mapper)
    {
        return importedModules.Select(importedModule => {
            return new Module{
                name = importedModule.name,
                prefab = importedModule.prefab,
                mesh = importedModule.mesh,
                faceIndices = MapFaceHashesToFaceIndices(importedModule.faceHashes, mapper),
                flippedFaceIndices = MapFaceHashesToFaceIndices(importedModule.flippedFaceHashes, mapper),
            };
        });
    }

    private static Module.FaceIndices MapFaceHashesToFaceIndices(ImportedModule.FaceHashes hashes, FaceLayoutHashToIndexMapper mapper)
    {
        return new Module.FaceIndices{
            top = mapper.ConvertVerticalHashesToIndices(hashes.top),
            bottom = mapper.ConvertVerticalHashesToIndices(hashes.bottom),
            sides = mapper.ConvertHorizontalHashesToIndices(hashes.sides),
        };
    }

    private static void WriteModulesToProperties(SerializedProperty modulesListProperty, IEnumerable<Module> modules)
    {
        foreach (var module in modules)
        {
            modulesListProperty.InsertArrayElementAtIndex(modulesListProperty.arraySize);
            var arrayElement = modulesListProperty.GetArrayElementAtIndex(modulesListProperty.arraySize - 1);
            WriteSingleModuleProperties(arrayElement, module);
        }
    }

    private static void WriteSingleModuleProperties(SerializedProperty moduleRootProperty, Module module)
    {
        moduleRootProperty.FindPropertyRelative(nameof(Module.name)).stringValue = module.name;
        moduleRootProperty.FindPropertyRelative(nameof(Module.prefab)).objectReferenceValue = module.prefab;

        var meshProperty = moduleRootProperty.FindPropertyRelative(nameof(Module.mesh));

        WriteModuleMeshToSerializedProperty(module.mesh, meshProperty);

        var indexedFacesProperty = moduleRootProperty.FindPropertyRelative(nameof(Module.faceIndices));
        var flippedIndexedFacesProperty = moduleRootProperty.FindPropertyRelative(nameof(Module.flippedFaceIndices));

        WriteIndexedFacesToSerializedProperty(module.faceIndices, indexedFacesProperty);
        WriteIndexedFacesToSerializedProperty(module.flippedFaceIndices, flippedIndexedFacesProperty);
    }

    private static void WriteTransformedModulesToProperties(SerializedProperty serializedMarchingCubeLookup, int[] moduleContents, int prefabIndex, float selectionWeight)
    {
        foreach (var flipped in new[] {true, false})
        {
            foreach (var yawIndex in Enumerable.Range(0,4))
            {
                WriteSingleTransformedModuleToProperties(serializedMarchingCubeLookup, moduleContents, prefabIndex, selectionWeight, flipped, yawIndex);
            }
        }
    }

    private static void WriteSingleTransformedModuleToProperties(SerializedProperty serializedMarchingCubeLookup, int[] moduleContents, int prefabIndex, float selectionWeight, bool flipped, int yawIndex)
    {
        var flippedLookupArray = flipped ? FlipMarchingCubeContentsAcrossX(moduleContents) : moduleContents;
        var flippedAndYawedContents = YawMarchingCubeContents(flippedLookupArray, yawIndex)
            .ToArray();
        var marchingCubeLookupIndex = GetMarchingCubeIndexForContents(flippedAndYawedContents);
        var lookupElement = serializedMarchingCubeLookup.GetArrayElementAtIndex(marchingCubeLookupIndex).FindPropertyRelative(nameof(Tileset.CubeConfiguration.availableModules));
        lookupElement.InsertArrayElementAtIndex(lookupElement.arraySize);
        var transformedModuleProperty = lookupElement.GetArrayElementAtIndex(lookupElement.arraySize - 1);
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.moduleIndex)).intValue = prefabIndex;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.isFlipped)).boolValue = flipped;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.yawIndex)).intValue = yawIndex;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.selectionWeight)).floatValue = selectionWeight;
    }

    private static int GetMarchingCubeIndexForContents(int[] vertexInsideness)
    {
        return vertexInsideness.Select((contents, index) => ConvertVertexContentsToZeroOneInsideness(contents) << index).Sum();
    }

    private static void WriteBlockModuleToProperties(SerializedProperty serializedMarchingCubeLookup, int marchingCubeIndex, int prefabIndex)
    {
        var lookupElement = serializedMarchingCubeLookup.GetArrayElementAtIndex(marchingCubeIndex).FindPropertyRelative(nameof(Tileset.CubeConfiguration.availableModules));
        lookupElement.InsertArrayElementAtIndex(lookupElement.arraySize);
        var transformedModuleProperty = lookupElement.GetArrayElementAtIndex(lookupElement.arraySize - 1);
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.moduleIndex)).intValue = prefabIndex;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.isFlipped)).boolValue = false;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.yawIndex)).intValue = 0;
        transformedModuleProperty.FindPropertyRelative(nameof(TransformedModule.selectionWeight)).floatValue = 1f;
    }

    private static FaceLayoutHashToIndexMapper.MapperAndMatchingFaceIndices CreateHashToIndexMapper
    (
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> horizontalHashes,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> verticalHashes
    )
    {
        return FaceLayoutHashToIndexMapper.CreateMapper(ToSimpleHashMappings(horizontalHashes), ToSimpleHashMappings(verticalHashes));
    }

    private static IEnumerable<KeyValuePair<FaceLayoutHash, FaceLayoutHash>> ToSimpleHashMappings(Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> workingHashMap)
    {
        return workingHashMap.Select(mapping => new KeyValuePair<FaceLayoutHash, FaceLayoutHash>(mapping.Key, mapping.Value.oppositeHash));
    }

    private static int ConvertVertexContentsToZeroOneInsideness(int insideness)
    {
        return insideness > 0 ? 1 : 0;
    }

    private static void WriteFaceIndicesToSerializedProperty(FaceLayoutIndex[] sourceData, SerializedProperty destinationArray)
    {
        destinationArray.arraySize = sourceData.Length;
        for (var i = 0; i < sourceData.Length; ++i)
        {
            var matchingFaceIndex = sourceData[i];
            var elementProperty = destinationArray.GetArrayElementAtIndex(i);
            elementProperty.FindPropertyRelative(FaceLayoutIndex.indexName).intValue = Convert.ToInt32(matchingFaceIndex.Index);
        }
    }

    private static void WriteIndexedFacesToSerializedProperty(Module.FaceIndices faceIndices, SerializedProperty faceIndicesProperty)
    {
        var topProperty = faceIndicesProperty.FindPropertyRelative(nameof(Module.FaceIndices.top));
        Assert.IsTrue(topProperty.isArray);
        var bottomProperty = faceIndicesProperty.FindPropertyRelative(nameof(Module.FaceIndices.bottom));
        Assert.IsTrue(bottomProperty.isArray);
        topProperty.arraySize = 4;
        bottomProperty.arraySize = 4;
        for (var yawIndex = 0; yawIndex < 4; ++yawIndex)
        {
            topProperty.GetArrayElementAtIndex(yawIndex).FindPropertyRelative(FaceLayoutIndex.indexName).intValue = (int)faceIndices.top[yawIndex].Index;
            bottomProperty.GetArrayElementAtIndex(yawIndex).FindPropertyRelative(FaceLayoutIndex.indexName).intValue = (int)faceIndices.bottom[yawIndex].Index;
        }

        var sidesProperty = faceIndicesProperty.FindPropertyRelative(nameof(ImportedModule.FaceHashes.sides));
        Assert.IsTrue(sidesProperty.isArray);
        sidesProperty.arraySize = 4;
        for (var sideIndex = 0; sideIndex < 4; ++sideIndex)
        {
            sidesProperty.GetArrayElementAtIndex(sideIndex).FindPropertyRelative(FaceLayoutIndex.indexName).intValue = (int)faceIndices.sides[sideIndex].Index;
        }
    }

    private static void WriteModuleMeshToSerializedProperty(ModuleMesh mesh, SerializedProperty meshProperty)
    {
        WriteArrayToSerializedProperty.Vector3Array(mesh.vertices, meshProperty.FindPropertyRelative(nameof(ModuleMesh.vertices)));
        WriteArrayToSerializedProperty.Vector3Array(mesh.normals, meshProperty.FindPropertyRelative(nameof(ModuleMesh.normals)));
        WriteArrayToSerializedProperty.Vector4Array(mesh.tangents, meshProperty.FindPropertyRelative(nameof(ModuleMesh.tangents)));
        WriteArrayToSerializedProperty.GenericArray(mesh.uvs, meshProperty.FindPropertyRelative(nameof(ModuleMesh.uvs)), (uvChannel, prop) => {
            WriteArrayToSerializedProperty.Vector4Array(uvChannel.fullWidthChannel, prop.FindPropertyRelative(nameof(ModuleMesh.UvChannel.fullWidthChannel)));
            prop.FindPropertyRelative(nameof(ModuleMesh.UvChannel.channelWidth)).intValue = uvChannel.channelWidth;
        });
        WriteArrayToSerializedProperty.IntArray(mesh.triangles, meshProperty.FindPropertyRelative(nameof(ModuleMesh.triangles)));
        WriteArrayToSerializedProperty.GenericArray(mesh.subMeshes, meshProperty.FindPropertyRelative(nameof(ModuleMesh.subMeshes)), (subMesh, prop) => {
            prop.FindPropertyRelative(nameof(ModuleMesh.SubMesh.startIndex)).intValue = subMesh.startIndex;
            prop.FindPropertyRelative(nameof(ModuleMesh.SubMesh.indicesCount)).intValue = subMesh.indicesCount;
            prop.FindPropertyRelative(nameof(ModuleMesh.SubMesh.material)).objectReferenceValue = subMesh.material;
        });

    }

    private static readonly int[] yaws = new []{0,1,2,3};
    private static readonly int[] counterClockwiseYaws = new []{0,3,2,1};
    private static readonly Face[] xzFaces = new []{Face.Forward, Face.Right, Face.Back, Face.Left};

    // only exposed for testing
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
    public static ImportedModule.FaceHashes HashFacesAndEnsureMatchingFaceInMap
    (
        ModuleConnectivityData moduleData,
        int positionHashResolution,
        int normalHashResolution,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutHorizontalMap,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutVerticalMap,
        DeepHashCollisionReporter horizontalHashCollisionReporter,
        DeepHashCollisionReporter verticalHashCollisionReporter,
        ModuleFaceIdentifier halfFilledFaceIdentifier
    )
    {
        ImportInfoLog.Log(() => "Hashing unflipped faces");

        ImportInfoLog.Log(() => "Hashing top");
        halfFilledFaceIdentifier.face = Face.Up;
        var top = counterClockwiseYaws.Select(yawIndex => HashAndEnsureOppositeFaceMappingInMapForVerticalFace(moduleData.connectiveFaces.up.edges, positionHashResolution, normalHashResolution, yawIndex, inoutVerticalMap, verticalHashCollisionReporter, halfFilledFaceIdentifier.WithYawIndex(yawIndex))).ToArray();

        ImportInfoLog.Log(() => "Hashing sides");
        var sides = xzFaces.Select(xzFace => HashAndEnsureOppositeFaceMappingInMap(moduleData.connectiveFaces[xzFace].edges, positionHashResolution, normalHashResolution, inoutHorizontalMap, FaceDirection.Horizontal, horizontalHashCollisionReporter, halfFilledFaceIdentifier.WithFace(xzFace))).ToArray();

        ImportInfoLog.Log(() => "Hashing bottom");
        halfFilledFaceIdentifier.face = Face.Down;
        var bottom = yaws.Select(yawIndex => HashAndEnsureOppositeFaceMappingInMapForVerticalFace(moduleData.connectiveFaces.down.edges, positionHashResolution, normalHashResolution, yawIndex, inoutVerticalMap, verticalHashCollisionReporter, halfFilledFaceIdentifier.WithYawIndex(yawIndex))).ToArray();

        return new ImportedModule.FaceHashes{
            top = top,
            sides = sides,
            bottom = bottom,
        };
    }

    private static IList<Edge> FlipEdges(IEnumerable<Edge> edges, Face face)
    {
        return edges.Select(edge => FlipEdge(edge, face)).ToList();
    }

    private static IList<Edge> FlipEdgesAndEdgeOrder(IEnumerable<Edge> edges, Face face)
    {
        // still start facing forward
        var reversedOrder = edges.Take(1).Concat(edges.Skip(1).Reverse());
        return FlipEdges(reversedOrder, face);
    }

    private static Edge FlipEdge(Edge edge, Face face)
    {
        var xFlipper2d = new Vector2(-1f, 1f);
        var flipper3d = new Vector3(-1f, 1f, 1f);

        return new Edge{
            start = Vector2.Scale(edge.end, xFlipper2d),
            startNormal = Vector3.Scale(edge.endNormal, flipper3d),

            end = Vector2.Scale(edge.start, xFlipper2d),
            endNormal = Vector3.Scale(edge.startNormal, flipper3d),

            material = edge.material,
        };
    }

    private static readonly Face[] counterClockwiseXzFaces = new []{Face.Forward, Face.Left, Face.Back, Face.Right};
    private static ImportedModule.FaceHashes HashFlippedFacesAndEnsureMatchingFaceInMap(
        ModuleConnectivityData moduleData,
        int positionHashResolution,
        int normalHashResolution,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutHorizontalMap,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutVerticalMap,
        DeepHashCollisionReporter horizontalHashCollisionReporter,
        DeepHashCollisionReporter verticalHashCollisionReporter,
        ModuleFaceIdentifier halfFilledFaceIdentifier
    )
    {
        ImportInfoLog.Log(() => "Hashing flipped faces");

        ImportInfoLog.Log(() => "Hashing top");
        halfFilledFaceIdentifier.face = Face.Up;
        var top = counterClockwiseYaws
                .Select(yawIndex => HashAndEnsureOppositeFaceMappingInMapForVerticalFace(FlipEdgesAndEdgeOrder(moduleData.connectiveFaces.up.edges, Face.Up), positionHashResolution, normalHashResolution, yawIndex, inoutVerticalMap, verticalHashCollisionReporter, halfFilledFaceIdentifier.WithYawIndex(yawIndex)))
                .ToArray();

        ImportInfoLog.Log(() => "Hashing sides");
        halfFilledFaceIdentifier.yawIndex = 0;
        var sides = counterClockwiseXzFaces
                .Select(xzFace => HashAndEnsureOppositeFaceMappingInMap(FlipEdgesAndEdgeOrder(moduleData.connectiveFaces[xzFace].edges, xzFace), positionHashResolution, normalHashResolution, inoutHorizontalMap, FaceDirection.Horizontal, horizontalHashCollisionReporter, halfFilledFaceIdentifier.WithFace(FlipXFace(xzFace))))
                .ToArray();

        ImportInfoLog.Log(() => "Hashing bottom");
        halfFilledFaceIdentifier.face = Face.Down;
        var bottom = yaws
                .Select(yawIndex => HashAndEnsureOppositeFaceMappingInMapForVerticalFace(FlipEdgesAndEdgeOrder(moduleData.connectiveFaces.down.edges, Face.Down), positionHashResolution, normalHashResolution, yawIndex, inoutVerticalMap, verticalHashCollisionReporter, halfFilledFaceIdentifier.WithYawIndex(yawIndex)))
                .ToArray();
        return new ImportedModule.FaceHashes{
            top = top,
            sides = sides,
            bottom = bottom,
        };
    }

    private static Face FlipXFace(Face face)
    {
        switch (face)
        {
        case Face.Left:
            return Face.Right;
        case Face.Right:
            return Face.Left;
        default:
            return face;
        }
    }

    private static FaceLayoutHash HashAndEnsureOppositeFaceMappingInMapForVerticalFace
    (
        IEnumerable<Edge> faceEdgesRaw,
        int positionHashResolution,
        int normalHashResolution,
        int yawIndex,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutMap,
        DeepHashCollisionReporter hashCollisionReporter,
        ModuleFaceIdentifier faceIdentifier
    )
    {
        var faceEdges = faceEdgesRaw.Select(edge => edge.RotatedAboutZ(yawIndex)).ToList();
        var faceHashes = HashAndEnsureOppositeFaceMappingInMap(faceEdges, positionHashResolution, normalHashResolution, inoutMap, FaceDirection.Vertical, hashCollisionReporter, faceIdentifier);
        return faceHashes;
    }

    private enum FaceDirection
    {
        Horizontal,
        Vertical,
    }

    private static FaceLayoutHash HashAndEnsureOppositeFaceMappingInMap
    (
        IList<Edge> faceEdges,
        int positionHashResolution,
        int normalHashResolution,
        Dictionary<FaceLayoutHash, FaceIdentifierAndOppositeHash> inoutMap,
        FaceDirection direction,
        DeepHashCollisionReporter deepHashCollisionReporter,
        ModuleFaceIdentifier faceIdentifier
    )
    {
        var hash = HashFaces.FromFaceEdges(faceEdges, positionHashResolution, normalHashResolution);
        var oppositeHash = direction == FaceDirection.Horizontal ?
            HashFaces.FromHorizontallyOppositeFaceEdges(faceEdges, positionHashResolution, normalHashResolution) :
            HashFaces.FromVerticallyOppositeFaceEdges(faceEdges, positionHashResolution, normalHashResolution);

        if (!inoutMap.ContainsKey(hash))
        {
            if (inoutMap.ContainsKey(oppositeHash))
            {
                deepHashCollisionReporter(oppositeHash, hash, faceIdentifier.WithOpposite(true)); 
            }
            else
            {
                inoutMap.Add(hash, new FaceIdentifierAndOppositeHash{
                    faceIdentifier = faceIdentifier,
                    oppositeHash = oppositeHash
                });

                // the hashes can be equal if you're compatible with yourself
                // e.g. you don't have any edges on a face
                if (!hash.Equals(oppositeHash))
                {
                    inoutMap.Add(oppositeHash, new FaceIdentifierAndOppositeHash{
                        faceIdentifier = faceIdentifier.WithOpposite(true),
                        oppositeHash = hash
                    });
                }
            }
        }
        else
        {
            var existingMatchingHash = inoutMap[hash];
            if (existingMatchingHash.oppositeHash.HashValue != oppositeHash.HashValue)
            {
                deepHashCollisionReporter(hash, oppositeHash, faceIdentifier); 
            }
        }

        return hash;
    }
    public delegate void HashCollisionReporter(FaceLayoutHash collidingHash, FaceLayoutHash newOppositeHash, ModuleFaceIdentifier newFace, FaceLayoutHash oldOppositeHash, ModuleFaceIdentifier oldFace);
    public delegate void DeepHashCollisionReporter(FaceLayoutHash collidingHash, FaceLayoutHash newOppositeHash, ModuleFaceIdentifier newOppositeFace);

    private static int[] FlipMarchingCubeContentsAcrossX(int[] original)
    {
        Assert.AreEqual(original.GetLength(0), 8);
        return Enumerable.Range(0,4).SelectMany(yzIndex => Enumerable.Range(0,2).Select(x => original[(yzIndex << 1) + (1-x)])).ToArray();
    }

    private static readonly int[,] yawContentsMapping = {
        {0,1,4,5},
        {1,5,0,4},
        {5,4,1,0},
        {4,0,5,1},
    };

    private static IEnumerable<int> YawMarchingCubeContents(int[] original, int yawIndex)
    {
        Assert.AreEqual(original.GetLength(0), 8);

        var smallPositiveYawIndex = ((yawIndex % 4) + 4) % 4;
        return Enumerable.Range(0,2).SelectMany(z => Enumerable.Range(0,2).SelectMany(y => Enumerable.Range(0,2).Select(x =>
        {
            return original[(y << 1) + yawContentsMapping[smallPositiveYawIndex, x + (z << 1)]];
        })));
    }

    private static void SaveTileDimensionsToDestination(TilesetImporterAsset importerTarget)
    {
        importerTarget.destinationTileset.tileDimensions = new Vector3(importerTarget.importerSettings.tileWidth, importerTarget.importerSettings.tileHeight, importerTarget.importerSettings.tileWidth);
    }

    private static void ClearOldPostprocessorsFromDestination(Tileset destinationTileset)
    {
        var destinationAssetPath = AssetDatabase.GetAssetPath(destinationTileset);
        var allTilesetAssets = AssetDatabase.LoadAllAssetsAtPath(destinationAssetPath);
        foreach (var subasset in allTilesetAssets)
        {
            if (subasset is Postprocessing.Postprocessor)
            {
                Undo.DestroyObjectImmediate(subasset); // this method does not have an "undo name" parameter. Would be undoOperationName
            }
        }
    }

    private static void CreateAndSavePostprocessorInDestination(TilesetImporterAsset importerTarget)
    {
        var createdPostprocessor = importerTarget.postprocessorCreator.CreatePostprocessor(importerTarget.importerSettings);
        Assert.IsNotNull(createdPostprocessor, $"Postprocessor creator {importerTarget.postprocessorCreator.GetType().Name} did not create a postprocessor");
        createdPostprocessor.name = createdPostprocessor.GetType().Name;
        Undo.RegisterCreatedObjectUndo(createdPostprocessor, undoOperationName);
        Undo.RegisterCompleteObjectUndo(createdPostprocessor, undoOperationName);
        AssetDatabase.AddObjectToAsset(createdPostprocessor, importerTarget.destinationTileset);

        {
            var serializedTileset = new SerializedObject(importerTarget.destinationTileset);

            var postprocessorProperty = serializedTileset.FindProperty(nameof(Tileset.postprocessor));
            postprocessorProperty.objectReferenceValue = createdPostprocessor;

            serializedTileset.ApplyModifiedProperties();
        }
    }

    private static float GetModuleSelectionWeight(ModuleAndConnectivity module)
    {
        if (module.name.ToLower().StartsWith("limited"))
        {
            return 0f;
        }
        
        return 1f;
    }

    private static void AddTrianglesOnModuleFacesToImportDetails(List<ModuleAndConnectivity> objectsAndConnections, ImportDetails importDetails)
    {
        importDetails.modulesWithFacesOnACubeFace.AddRange(
            objectsAndConnections
                .Select((objectToConnectivity, index) => new {objectToConnectivity, index})
                .Where(objectToConnectivityAndIndex => objectToConnectivityAndIndex.objectToConnectivity.connectivity.trianglesOnModuleBoundsFace.Count > 0)
                .Select(objectToConnectivityAndIndex => new ImportDetails.ModuleWithFacesOnACubeFace{
                    moduleIndex = objectToConnectivityAndIndex.index,
                    cachedName = objectToConnectivityAndIndex.objectToConnectivity.name,
                    numberOfFacesOnCubeFace = objectToConnectivityAndIndex.objectToConnectivity.connectivity.trianglesOnModuleBoundsFace.Count,
                })
        );
    }

    private static void AddSuperInsideOutsideCornersToImportDetails(List<ModuleAndConnectivity> objectsAndConnections, ImportDetails importDetails)
    {
        importDetails.superInsideCornerModules.AddRange(
            objectsAndConnections
                .Select((objectToConnectivity, index) => new {objectToConnectivity, index, firstSuperCorner = Array.FindIndex(objectToConnectivity.connectivity.moduleVertexContents, IsSuperInsideOutsideCornerContents)})
                .Where(objectToConnectivityAndIndexAndFirstCornerContents => objectToConnectivityAndIndexAndFirstCornerContents.firstSuperCorner != -1)
                .Select(objectToConnectivityAndIndexAndFirstCornerContents => new ImportDetails.SuperInsideCornerModule{
                    moduleIndex = objectToConnectivityAndIndexAndFirstCornerContents.index,
                    cachedName = objectToConnectivityAndIndexAndFirstCornerContents.objectToConnectivity.name,
                    firstSuperInsideOutsideCornerIndex = objectToConnectivityAndIndexAndFirstCornerContents.firstSuperCorner,
                })
        );
    }

    private static void AddOutOfBoundsModulesToImportDetails(List<ModuleAndConnectivity> objectsAndConnections, ImportDetails importDetails)
    {
        importDetails.outOfBoundsModules.AddRange(
            objectsAndConnections
                .Where(objectToConnectivity => objectToConnectivity.connectivity.vertexIndicesOutsideOfBounds.Count > 0)
                .Select(objectToConnectivity => new ImportDetails.OutOfBoundsModule{
                    cachedName = objectToConnectivity.name,
                })
        );
    }

    private static bool IsSuperInsideOutsideCornerContents(int cornerContents)
    {
        return cornerContents != 0 && cornerContents != 1;
    }
}

}

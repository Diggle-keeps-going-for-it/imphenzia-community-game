using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
internal static class ConnectedModulesListView
{
    internal delegate Tileset GetTileset();
    internal static void SetUpListView(ListView connectedModulesField, StyleSheet invalidConnectedModuleStyle, StyleSheet invalidMatchingContentsConnectedModuleStyle, GetTileset getTileset)
    {
        Assert.IsNotNull(getTileset);
        ListViewBinder.SetupListViewInitial<ConnectedModule>(connectedModulesField, (uiElement, connectedModule) => 
        {
            (uiElement as Label).text = CreateNameFromTransformedModule(getTileset(), connectedModule.transformedModule);

            if (connectedModule.validity == ConnectedModule.ModuleValidity.MatchingContentsButMismatchingFaces)
            {
                EnsureElementHasStyle(uiElement, invalidConnectedModuleStyle);
            }
            else
            {
                EnsureElementDoesNotHaveStyle(uiElement, invalidConnectedModuleStyle);
            }

            if (connectedModule.validity == ConnectedModule.ModuleValidity.MismatchingContents)
            {
                EnsureElementHasStyle(uiElement, invalidMatchingContentsConnectedModuleStyle);
            }
            else
            {
                EnsureElementDoesNotHaveStyle(uiElement, invalidMatchingContentsConnectedModuleStyle);
            }
        });
    }

    internal static void SelectConnectedModuleInUi(ListView connectedModulesField, Tileset tileset, CachedConnectedModuleIdentifier connectedModuleIdentifier)
    {
        var connectedModuleIndex = GetConnectedModuleIndexByIdentifier(connectedModulesField, tileset, connectedModuleIdentifier);
        connectedModulesField.SetSelection(connectedModuleIndex);
        // need to wait for Unity to take a frame to populate the list view before we can scroll to it reliably
        // do it this frame if possible, and with a delay in case Unity needs to populate the list view first
        connectedModulesField.ScrollToItem(connectedModuleIndex);
        EditorApplication.delayCall += () => connectedModulesField.ScrollToItem(connectedModuleIndex);
    }

    private static int GetConnectedModuleIndexByIdentifier(ListView connectedModulesField, Tileset tileset, SubInspectors.ConnectedModules.CachedConnectedModuleIdentifier connectedModuleIdentifier)
    {
        if (connectedModuleIdentifier != null)
        {
            var foundTransformedModuleIndex = FindModuleIndexInConnectedModulesField(connectedModulesField, tileset, connectedModuleIdentifier.prefabName, connectedModuleIdentifier.yawIndex, connectedModuleIdentifier.isFlipped);
            return foundTransformedModuleIndex != -1 ? foundTransformedModuleIndex : 0;
        }
        else
        {
            return 0;
        }
    }

    private static int FindModuleIndexInConnectedModulesField(ListView connectedModulesField, Tileset tileset, string moduleName, int yawIndex, bool isFlipped)
    {
        if (tileset == null)
        {
            return -1;
        }
        else
        {
            var foundRawModuleIndex = tileset.modules.FindIndex(module => module.name == moduleName);
            return ((List<ConnectedModule>)connectedModulesField.itemsSource).FindIndex(
                module => 
                    module.transformedModule.moduleIndex == foundRawModuleIndex && 
                    module.transformedModule.yawIndex == yawIndex &&
                    module.transformedModule.isFlipped == isFlipped);
        }
    }

    internal static CachedConnectedModuleIdentifier CreateConnectedModuleIdentifier(Tileset tileset, ConnectedModule connectedModule)
    {
        if (connectedModule == null)
        {
            return null;
        }

        var result = new CachedConnectedModuleIdentifier();

        result.prefabName = tileset.modules[connectedModule.transformedModule.moduleIndex].name;
        result.yawIndex = connectedModule.transformedModule.yawIndex;
        result.isFlipped = connectedModule.transformedModule.isFlipped;

        return result;
    }

    internal static void PopulateConnectedModulesField(ListView connectedModulesField, SelectedModule currentModule, Tileset tileset, Face direction, PersistentData.ModuleValidity moduleValidity)
    {
        ListViewBinder.BindListView(connectedModulesField, GetConnectedModules(currentModule, tileset, direction, moduleValidity));
    }

    private static List<ConnectedModule> GetConnectedModules(SelectedModule currentModule, Tileset tileset, Face direction, PersistentData.ModuleValidity moduleValidity)
    {
        if (currentModule != null)
        {
            var faceDefinition = Calculator.DirectionToConnectiveFace(direction);
            var serializableFaceDefinition = Array.Find(FaceDataSerialization.serializationFaces, serFaceDef => serFaceDef.face == faceDefinition.face);
            if (tileset?.MarchingCubeLookup != null)
            {
                var basicFaceIndices = TransformedModule.GetFaceLayoutIndices(currentModule.module, false, 0);
                var faceIndex = basicFaceIndices[faceDefinition.face];
                var directionalMatchingFaceMappings = faceDefinition.offsetDirection == FaceDefinition.OffsetDirection.Vertical ? tileset.verticalMatchingFaceLayoutIndices : tileset.horizontalMatchingFaceLayoutIndices;
                var requiredOppositeFaceIndex = SafeGetMatchingFaceIndex(directionalMatchingFaceMappings, faceIndex);

                var oppositeFace = FaceDataSerialization.GetOppositeFace(faceDefinition.face);

                switch (moduleValidity)
                {
                    case SubInspectors.ConnectedModules.PersistentData.ModuleValidity.ValidOnly:
                    {
                        return GetAllValidTransformedModules(tileset, currentModule, oppositeFace, serializableFaceDefinition, requiredOppositeFaceIndex);
                    }
                    case SubInspectors.ConnectedModules.PersistentData.ModuleValidity.MatchingVertexContents:
                    {
                        return GetAllMarkedUpMatchingFaceContentsTransformedModules(tileset, currentModule, oppositeFace, serializableFaceDefinition, requiredOppositeFaceIndex);
                    }
                    case SubInspectors.ConnectedModules.PersistentData.ModuleValidity.All:
                    {
                        return GetAllMarkedUpTransformedModules(tileset, currentModule, oppositeFace, faceDefinition, serializableFaceDefinition, requiredOppositeFaceIndex);
                    }
                }
            }
        }

        return new List<ConnectedModule>();
    }

    private static FaceLayoutIndex? SafeGetMatchingFaceIndex(FaceLayoutIndex[] matchingMap, FaceLayoutIndex index)
    {
        var indexRaw = index.Index;
        Assert.IsTrue(indexRaw >= 0);
        if (indexRaw < matchingMap.Length)
        {
            return matchingMap[indexRaw];
        }
        else
        {
            return null;
        }
    }

    private static List<ConnectedModule> GetAllValidTransformedModules(Tileset tileset, SelectedModule currentModule, Face oppositeFace, FaceDataSerialization.SerializationFace serializableFaceDefinition, FaceLayoutIndex? requiredOppositeFaceIndex)
    {
        return GetAllMatchingFaceContentsTransformedModulesFromConfig(tileset, currentModule.contents, serializableFaceDefinition)
            .Where(transformedModule => IsTransformedModuleValid(transformedModule, tileset, oppositeFace, requiredOppositeFaceIndex))
            .Select(transformedModule => new ConnectedModule{
                transformedModule = transformedModule,
                validity = ConnectedModule.ModuleValidity.Valid
            }).ToList();
    }

    private static List<ConnectedModule> GetAllMarkedUpMatchingFaceContentsTransformedModules(Tileset tileset, SelectedModule currentModule, Face oppositeFace, FaceDataSerialization.SerializationFace serializableFaceDefinition, FaceLayoutIndex? requiredOppositeFaceIndex)
    {
        return GetAllMatchingFaceContentsTransformedModulesFromConfig(tileset, currentModule.contents, serializableFaceDefinition)
            .Select(transformedModule => new ConnectedModule{
                transformedModule = transformedModule,
                validity = IsTransformedModuleValid(transformedModule, tileset, oppositeFace, requiredOppositeFaceIndex)
                    ? ConnectedModule.ModuleValidity.Valid
                    : ConnectedModule.ModuleValidity.MatchingContentsButMismatchingFaces,
            }).ToList();
    }

    private static List<ConnectedModule> GetAllMarkedUpTransformedModules(Tileset tileset, SelectedModule currentModule, Face oppositeFace, FaceDefinition faceDefinition, FaceDataSerialization.SerializationFace serializableFaceDefinition, FaceLayoutIndex? requiredOppositeFaceIndex)
    {
        return GetAllTransformedModulesFromConfig(tileset, currentModule.contents, faceDefinition.face)
            .Select(transformedModule => new ConnectedModule{
                transformedModule = transformedModule.module,
                validity = transformedModule.contentsMatch
                    ? (IsTransformedModuleValid(transformedModule.module, tileset, oppositeFace, requiredOppositeFaceIndex)
                        ? ConnectedModule.ModuleValidity.Valid
                        : ConnectedModule.ModuleValidity.MatchingContentsButMismatchingFaces)
                    : ConnectedModule.ModuleValidity.MismatchingContents,
            }).ToList();
    }

    private static bool IsTransformedModuleValid(TransformedModule transformedModule, Tileset tileset, Face oppositeFace, FaceLayoutIndex? requiredOppositeFaceIndex)
    {
        var transformedModuleFaceIndices = transformedModule.GetFaceLayoutIndices(tileset.modules);
        var facingFaceIndex = transformedModuleFaceIndices[oppositeFace];
        var faceIndexMatches = facingFaceIndex.Equals(requiredOppositeFaceIndex);
        return faceIndexMatches;
    }

    private static string CreateNameFromTransformedModule(Tileset tileset, TransformedModule transformedModule)
    {
        Assert.IsNotNull(transformedModule);

        var moduleName = TilesetInspector.GetModuleNameFromModuleIndex(tileset, transformedModule.moduleIndex);

        return $"{moduleName} yaw {transformedModule.yawIndex * 90}{(transformedModule.isFlipped ? " flipped" : "")}";
    }

    private static void EnsureElementHasStyle(VisualElement element, StyleSheet style)
    {
        if (!element.styleSheets.Contains(style))
        {
            element.styleSheets.Add(style);
        }
    }

    private static void EnsureElementDoesNotHaveStyle(VisualElement element, StyleSheet style)
    {
        if (element.styleSheets.Contains(style))
        {
            element.styleSheets.Remove(style);
        }
    }

    private static IEnumerable<MaybeInconsistentContentsTransformedModule> GetAllTransformedModulesFromConfig(Tileset stackConfig, int currentModuleContents, Face face)
    {
        var sourceFaceContentsBits = Array.Find(FaceDataSerialization.serializationFaces, faceData => faceData.face == face).contentsBitIndices;
        var remoteFace = FaceDataSerialization.GetOppositeFace(face);
        var remoteFaceContentsBits = Array.Find(FaceDataSerialization.serializationFaces, faceData => faceData.face == remoteFace).contentsBitIndices;

        var requiredRemoteContentsValue = 0;
        var remoteMask = 0;
        for (var bitIndex = 0; bitIndex < 4; ++bitIndex)
        {
            requiredRemoteContentsValue |= ((currentModuleContents >> sourceFaceContentsBits[bitIndex]) & 1) << remoteFaceContentsBits[bitIndex];
            remoteMask |= 1 << remoteFaceContentsBits[bitIndex];
        }

        foreach (var cubeConfigIndex in Enumerable.Range(0, 256))
        {
            var doContentsMatch = (cubeConfigIndex & remoteMask) == requiredRemoteContentsValue;
            foreach (var transformedModule in stackConfig.MarchingCubeLookup[cubeConfigIndex].availableModules)
            {
                yield return new MaybeInconsistentContentsTransformedModule{
                    module = transformedModule,
                    contentsMatch = doContentsMatch,
                };
            }
        }
    }

    private static IEnumerable<TransformedModule> GetAllMatchingFaceContentsTransformedModulesFromConfig(Tileset stackConfig, int moduleContents, FaceDataSerialization.SerializationFace face)
    {
        foreach (var matchingContents in FaceDataSerialization.EnumerateMatchingVertexContents(moduleContents, face))
        {
            foreach (var transformedModule in stackConfig.MarchingCubeLookup[matchingContents].availableModules)
            {
                yield return transformedModule;
            }
        }
    }

    public static void ClearConnectedModulesField(ListView connectedModulesField)
    {
        ListViewBinder.BindListView(connectedModulesField, new List<ConnectedModule>());
    }

    private struct MaybeInconsistentContentsTransformedModule
    {
        public TransformedModule module;
        public bool contentsMatch;
    }
}
}

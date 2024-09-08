using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;
using ConnectiveEdges = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveEdges;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
internal static class DiagnosticsDrawerCreator
{
    public static IDiagnosticsDrawer CreateDiagnosticsDrawer
    (
        TilesetImporterAsset tilesetImporter,
        CurrentModule currentModule,
        ConnectedModule connectedModuleInstance,
        SubInspectors.ConnectedModules.PersistentData persistentData
    )
    {
        if (tilesetImporter != null)
        {
            Assert.IsNotNull(tilesetImporter.importerSettings);
            var tileset = tilesetImporter.destinationTileset;
            if (tileset != null)
            {
                Assert.IsNotNull(tileset.modules);

                if (currentModule.module != null && currentModule.module.module != null && currentModule.module.module.mesh != null)
                {
                    var currentModuleConnectivity = Calculator.GetConnectivityFromMesh(currentModule.module.module.mesh, tilesetImporter.importerSettings.materialImportSettings, currentModule.module.module.name);
                    var moduleDrawerTransform = Matrix4x4.identity;

                    var connectedModuleMesh = GetMeshForConnectedModule(connectedModuleInstance, tileset.modules);
                    // user may have deleted the module or prefab but it's still present in the config/our UI
                    if (connectedModuleMesh != null)
                    {
                        var connectedModuleConnectivity = Calculator.GetConnectivityFromMesh(connectedModuleMesh, tilesetImporter.importerSettings.materialImportSettings, null);

                        if (connectedModuleInstance.validity != ConnectedModule.ModuleValidity.Valid)
                        {
                            var faceGetter = Calculator.DirectionToConnectiveFace(persistentData.direction);
                            var faceEdges = currentModuleConnectivity.connectiveFaces[faceGetter.face].edges;

                            var connectedFaces = connectedModuleConnectivity.connectiveFaces;
                            YawAndFlipFaceEdges(ref connectedFaces, connectedModuleInstance.transformedModule.isFlipped, connectedModuleInstance.transformedModule.yawIndex);
                            var connectedFaceEdges = connectedFaces[FaceDataSerialization.GetOppositeFace(faceGetter.face)].edges;
                            var oppositeConnectedFaceEdges = faceGetter.offsetDirection == Calculator.ConnectiveFaceDefinition.OffsetDirection.Horizontal
                                ? connectedFaceEdges.Select(edge => HashFaces.HorizontallyInvertFaceEdge(edge)).ToList()
                                : connectedFaceEdges.Select(edge => HashFaces.VerticallyInvertFaceEdge(edge)).ToList();

                            if (!DoVerticesMatch(faceEdges, oppositeConnectedFaceEdges, tilesetImporter, persistentData.direction, out var vertexMismatchDrawer))
                            {
                                return vertexMismatchDrawer;
                            }
                            else if (!DoEdgesMatch(faceEdges, oppositeConnectedFaceEdges, tilesetImporter, persistentData.direction, out var edgesMismatchDrawer))
                            {
                                return edgesMismatchDrawer;
                            }
                            else if (!DoNormalsMatch(faceEdges, oppositeConnectedFaceEdges, tilesetImporter, persistentData.direction, out var normalsMismatchDrawer))
                            {
                                return normalsMismatchDrawer;
                            }
                            else if (!DoMaterialsMatch(faceEdges, oppositeConnectedFaceEdges, tilesetImporter, persistentData.direction, out var materialsMismatchDrawer))
                            {
                                return materialsMismatchDrawer;
                            }
                            else
                            {
                                return new UnknownMismatchDrawer(GetTileWorldDelta(tilesetImporter, persistentData.direction), tilesetImporter.importerSettings.TileDimensions);
                            }
                        }
                        else
                        {
                            return new ValidConnectionsDrawer(GetTileWorldDelta(tilesetImporter, persistentData.direction), tilesetImporter.importerSettings.TileDimensions);
                        }
                    }
                }
            }
        }

        return null;
    }

    private static ModuleMesh GetMeshForConnectedModule(ConnectedModule connectedModule, IList<Module> modules)
    {
        var maybeConnectedModuleIndex = connectedModule?.transformedModule?.moduleIndex;

        if (maybeConnectedModuleIndex.HasValue)
        {
            var connectedModuleIndex = maybeConnectedModuleIndex.Value;
            if (connectedModuleIndex >= 0 && connectedModuleIndex < modules.Count)
            {
                return modules[connectedModuleIndex]?.mesh;
            }
        }

        return null;
    }

    internal static void TrySendConnectedModuleToStage(TilesetImporterAsset tilesetImporter, TilesetInspectorStage stage, SubInspectors.ConnectedModules.PersistentData persistentData, ConnectedModule connectedModule)
    {
        if (stage)
        {
            SendConnectedModuleToStage(tilesetImporter, stage, persistentData, connectedModule);
        }
    }

    internal static void SendConnectedModuleToStage(TilesetImporterAsset tilesetImporter, TilesetInspectorStage stage, SubInspectors.ConnectedModules.PersistentData persistentData, ConnectedModule connectedModule)
    {
        try
        {
            var deltaMultiplier = connectedModule.validity == ConnectedModule.ModuleValidity.Valid ? 1f : UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier;

            var module = tilesetImporter.destinationTileset.modules[connectedModule.transformedModule.moduleIndex];
            var direction = persistentData.direction;
            stage.ConnectedModuleSelected(GetTileWorldDelta(tilesetImporter, direction) * deltaMultiplier, module.prefab, module.mesh, tilesetImporter.importerSettings.TileDimensions, connectedModule.transformedModule.yawIndex, connectedModule.transformedModule.isFlipped, connectedModule.validity == ConnectedModule.ModuleValidity.Valid);
        }
        catch (Exception)
        {
            stage.ConnectedModuleSelected(Vector3.zero, null, null, tilesetImporter.importerSettings.TileDimensions, 0, false, false);
        }
    }

    private static Vector3Int DirectionToDelta(Face direction)
    {
        return Calculator.DirectionToConnectiveFace(direction)?.gridOffset ?? Vector3Int.zero;
    }

    private static void YawAndFlipFaceEdges(ref FaceData<ConnectiveEdges> faceEdges, bool isFlipped, int yawIndex)
    {
        if (isFlipped)
        {
            FlipFaceEdges(ref faceEdges);
        }

        YawFaceEdges(ref faceEdges, yawIndex);
    }

    private static void FlipFaceEdges(ref FaceData<ConnectiveEdges> faceEdges)
    {
        foreach (var face in FaceDataSerialization.serializationFaces)
        {
            var edges = faceEdges[face.face].edges;
            for (var edgeIndex = 0; edgeIndex < edges.Count; ++edgeIndex)
            {
                edges[edgeIndex] = edges[edgeIndex].FlippedAcrossX();
            }
        }

        (faceEdges[Face.Right], faceEdges[Face.Left]) = (faceEdges[Face.Left], faceEdges[Face.Right]);
    }

    private static void YawFaceEdges(ref FaceData<ConnectiveEdges> faceEdges, int yawIndex)
    {
        YawSingleFaceEdges(faceEdges[Face.Up].edges, -yawIndex);
        YawSingleFaceEdges(faceEdges[Face.Down].edges, yawIndex);

        var cachedFaceEdges = faceEdges;

        foreach (var face in new[]{Face.Forward, Face.Right, Face.Back, Face.Left})
        {
            var destinationFace = FaceDataSerialization.GetClockwiseFace(face, yawIndex);
            faceEdges[destinationFace] = cachedFaceEdges[face];
        }
    }

    private static void YawSingleFaceEdges(List<Edge> faceEdges, int yawIndex)
    {
        for (var edgeIndex = 0; edgeIndex < faceEdges.Count; ++edgeIndex)
        {
            faceEdges[edgeIndex] = faceEdges[edgeIndex].RotatedAboutZ(yawIndex);
        }
    }

    private static bool DoVerticesMatch(List<Edge> currentFaceEdges, List<Edge> connectedFaceEdges, TilesetImporterAsset tilesetImporter, Face direction, out VerticesMismatchDrawer result)
    {
        var faceVerts = currentFaceEdges.SelectMany(edge => new Vector2[]{edge.start, edge.end}).Distinct().ToList();
        var connectedFaceVerts = connectedFaceEdges.SelectMany(edge => new Vector2[]{edge.start, edge.end}).Distinct();

        var resolution = tilesetImporter.importerSettings.positionHashResolution;

        var comparisonResult = TilesetInspectorComparison.Compare(
            faceVert => HashFaces.HashQuantizedPosition(faceVert, resolution),
            faceVerts, connectedFaceVerts
        );

        if (comparisonResult.invalidComponentsOnCurrent.Count != 0 || comparisonResult.invalidComponentsOnConnected.Count != 0)
        {
            result = new VerticesMismatchDrawer(
                GetTileWorldDelta(tilesetImporter, direction), tilesetImporter.importerSettings.TileDimensions,
                comparisonResult.validComponents,
                comparisonResult.invalidComponentsOnCurrent,
                comparisonResult.invalidComponentsOnConnected,
                Calculator.DirectionToConnectiveFace(direction)
            );
            return false;
        }
        else
        {
            result = null;
            return true;
        }
    }

    private class EdgesAndWindingsMismatchResult
    {
        public List<Edge> matchingEdges = new List<Edge>();
        public List<Edge> invalidEdgesOnCurrent = new List<Edge>();
        public List<Edge> invalidEdgesOnDestination = new List<Edge>();
    }

    private static bool DoEdgesMatch(List<Edge> faceEdges, List<Edge> connectedFaceEdges, TilesetImporterAsset tilesetImporter, Face direction, out EdgesMismatchDrawer result)
    {
        var resolution = tilesetImporter.importerSettings.positionHashResolution;

        var comparisonResult = TilesetInspectorComparison.Compare(
            faceEdge => HashFaces.HashEdgePositions(faceEdge, resolution),
            faceEdges, connectedFaceEdges
        );

        if (comparisonResult.invalidComponentsOnCurrent.Count == 0 && comparisonResult.invalidComponentsOnConnected.Count == 0)
        {
            result = null;
            return true;
        }
        else
        {
            result = new EdgesMismatchDrawer(
                GetTileWorldDelta(tilesetImporter, direction),
                tilesetImporter.importerSettings.TileDimensions,
                comparisonResult.validComponents,
                comparisonResult.invalidComponentsOnCurrent,
                comparisonResult.invalidComponentsOnConnected,
                Calculator.DirectionToConnectiveFace(direction)
            );
            return false;
        }
    }

    private static bool DoNormalsMatch(List<Edge> currentFaceEdges, List<Edge> connectedFaceEdges, TilesetImporterAsset tilesetImporter, Face direction, out NormalsMismatchDrawer result)
    {
        var positionResolution = tilesetImporter.importerSettings.positionHashResolution;
        var normalResolution = tilesetImporter.importerSettings.normalHashResolution;
        var comparisonResult = TilesetInspectorComparison.CompareAfterMatching(
            faceEdge => HashFaces.HashEdgePositions(faceEdge, positionResolution),
            faceEdge => HashFaces.HashEdgePositions(faceEdge, positionResolution) ^ HashFaces.HashEdgeNormals(faceEdge, normalResolution),
            currentFaceEdges, connectedFaceEdges
        );

        if (comparisonResult.IsCompletelyValid)
        {
            result = null;
            return true;
        }
        else
        {
            result = new NormalsMismatchDrawer(
                GetTileWorldDelta(tilesetImporter, direction), tilesetImporter.importerSettings.TileDimensions,
                comparisonResult.validComponents,
                comparisonResult.invalidPriorMatchedComponents,
                comparisonResult.invalidComponentsOnCurrent,
                comparisonResult.invalidComponentsOnConnected,
                Calculator.DirectionToConnectiveFace(direction),
                tilesetImporter.importerSettings.normalHashResolution
            );
            return false;
        }
    }

    private static bool DoMaterialsMatch(List<Edge> currentFaceEdges, List<Edge> connectedFaceEdges, TilesetImporterAsset tilesetImporter, Face direction, out MaterialsMismatchDrawer result)
    {
        var positionResolution = tilesetImporter.importerSettings.positionHashResolution;
        var normalResolution = tilesetImporter.importerSettings.normalHashResolution;
        var comparisonResult = TilesetInspectorComparison.CompareAfterMatching(
            faceEdge => HashFaces.HashEdgePositions(faceEdge, positionResolution) ^ HashFaces.HashEdgeNormals(faceEdge, normalResolution),
            faceEdge => HashFaces.HashEdgePositions(faceEdge, positionResolution) ^ HashFaces.HashEdgeNormals(faceEdge, normalResolution) ^ HashFaces.HashEdgeMaterial(faceEdge),
            currentFaceEdges, connectedFaceEdges
        );

        if (comparisonResult.IsCompletelyValid)
        {
            result = null;
            return true;
        }
        else
        {
            result = new MaterialsMismatchDrawer(
                GetTileWorldDelta(tilesetImporter, direction),
                tilesetImporter.importerSettings.TileDimensions,
                comparisonResult.validComponents,
                comparisonResult.invalidPriorMatchedComponents,
                comparisonResult.invalidComponentsOnCurrent,
                comparisonResult.invalidComponentsOnConnected,
                Calculator.DirectionToConnectiveFace(direction)
            );
            return false;
        }
    }

    private static Vector3 GetTileWorldDelta(TilesetImporterAsset tilesetImporter, Face direction)
    {
        var intDelta = DirectionToDelta(direction);
        return Vector3.Scale((Vector3)intDelta, tilesetImporter.importerSettings.TileDimensions);
    }
}
}

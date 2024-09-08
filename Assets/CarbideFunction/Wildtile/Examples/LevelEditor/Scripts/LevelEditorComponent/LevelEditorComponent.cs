using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CarbideFunction.Wildtile;

public class LevelEditorComponent : MonoBehaviour
{
    [SerializeField]
    private GridPlacer firstPlacer = null;
    [SerializeField]
    private GridPlacer secondPlacer = null;

    private bool mostRecentlyGeneratedPlacerIsFirst = true;
    private GridPlacer MostRecentlyGeneratedPlacer => mostRecentlyGeneratedPlacerIsFirst ? firstPlacer : secondPlacer;
    private GridPlacer NextGeneratingPlacer => mostRecentlyGeneratedPlacerIsFirst ? secondPlacer : firstPlacer;

    [SerializeField]
    [Layer]
    private int voxelGridLayer = 0;

    [SerializeField]
    private Material debugVoxelGridMaterial = null;

    [SerializeField]
    private DualGridVoxelMeshPlaceholderTiles placeholderTiles = null;

    [SerializeField]
    private MouseFaceHighlighter mouseFaceHighlighter = new MouseFaceHighlighter();

    private VoxelMesher voxelMesher;

    [SerializeField]
    private Vector3Int editableAreaSize = Vector3Int.zero;

    private bool isCurrentlyGenerating = false;
    private bool isGenerationQueued = false;

    private void Awake()
    {
        Assert.IsNotNull(firstPlacer);
        Assert.IsNotNull(secondPlacer);
    }

    public void SetMap(IVoxelGrid voxelGrid)
    {
        Assert.IsNull(workingMap, "Setting the working map multiple times");
        workingMap = voxelGrid.Clone();
        firstPlacer.voxelGrid = (VoxelGridAsset)workingMap;
    }

    private GridPlacer.OnGenerationComplete onGenerationComplete = null;
    public void RegisterForGenerationComplete(GridPlacer.OnGenerationComplete onGenerationComplete)
    {
        this.onGenerationComplete = onGenerationComplete;
    }

    private void Start()
    {
        SetUpCollapseEventPropagators();
        SetMap(firstPlacer.voxelGrid);
        firstPlacer.voxelGrid = (VoxelGridAsset)workingMap;
        firstPlacer.GenerateOverTime();
        CreateVoxelMesh();
        CacheSlotGrid();
    }

    private void CacheSlotGrid()
    {
        var gridBuilder = new GridBuilderVisitor(firstPlacer.Tileset);
        workingMap.Visit(gridBuilder);
        cachedSlotGrid = gridBuilder.slotGrid;
    }

    public void HighlightMousePosition(Camera camera, Vector2 screenPosition)
    {
        var face = GetFaceForClick(camera, screenPosition, voxelMesher);
        var isValid = face.HasValue ? face.Value.facingVoxelIndex != IVoxelGrid.outOfBoundsVoxelIndex : false;
        mouseFaceHighlighter.HighlightFace(face, isValid);
    }

    public void DisableHighlight()
    {
        mouseFaceHighlighter.DisableHighlight();
    }

    private void CreateVoxelMesh()
    {
        voxelMesher = new VoxelMesher();
        voxelMesher.CreateMeshedVoxels(firstPlacer.voxelGrid, Tileset.TileDimensions, debugVoxelGridMaterial, debugVoxelGridMaterial, debugVoxelGridMaterial, firstPlacer.transform, voxelGridLayer);
    }

    public delegate void TilePlaced();
    public TilePlaced tilePlaced;

    public void AddTileAtCursor(Camera camera, Vector2 screenPosition)
    {
            var maybeFace = GetFaceForClick(camera, screenPosition, voxelMesher);
            if (maybeFace.HasValue)
            {
                SetTileContents(maybeFace.Value.facingVoxelIndex, true);
                tilePlaced?.Invoke();
            }
    }

    public void RemoveTileAtCursor(Camera camera, Vector2 screenPosition)
    {
            var maybeFace = GetFaceForClick(camera, screenPosition, voxelMesher);
            if (maybeFace.HasValue)
            {
                SetTileContents(maybeFace.Value.voxelIndex, false);
            }
    }

    private VoxelMesher.FaceData? GetFaceForClick(Camera camera, Vector2 screenPosition, VoxelMesher voxelMesher)
    {
        return VoxelFaceFinder.GetFaceForClick(camera, screenPosition, voxelMesher);
    }

    public IVoxelGrid Map => workingMap;
    private IVoxelGrid workingMap;
    public Tileset Tileset => firstPlacer.Tileset;
    
    private static Vector3Int CalculateSlotCoordinate(Vector3Int voxelCoordinate, GridPlacer targetPlacer)
    {
        return voxelCoordinate;
    }

    private void SetTileContents(int voxelIndex, bool newIsFilled)
    {
        if (voxelIndex != IVoxelGrid.outOfBoundsVoxelIndex)
        {
            if ((Map.GetVoxel(voxelIndex).contents != 0) != newIsFilled)
            {
                Map.SetVoxelContents(voxelIndex, newIsFilled ? 1 : 0);

                // for each of the 8 connected dual grid slots 
                Assert.IsNotNull(MostRecentlyGeneratedPlacer);
                Assert.IsNotNull(MostRecentlyGeneratedPlacer.transform.GetComponentInChildren<PlacedModulesRecord>(), "PlacedModulesRecord was missing from the GridPlacer's spawned objects. Change your TilesetImporterAsset's Postprocessor Creator to \"Example Level Editor Postprocessor Creator\" and click \"Process Models\" to fix this.");
                var placedModulesComponent = MostRecentlyGeneratedPlacer.transform.GetComponentInChildren<PlacedModulesRecord>();
                var placedModules = placedModulesComponent.placedModules;
                var rootTransform = placedModules[0].instance.transform.parent;

                var changedSlotIndices = placedModulesComponent.voxelIndexToSlotIndices[voxelIndex];

                foreach (var slotIndex in changedSlotIndices)
                {
                    var moduleData = placedModules[slotIndex];
                    var contents = Map.GetCubeContents(moduleData.sourceVoxels);
                    ReplaceSlotWithPlaceholder(placedModulesComponent.placedModules, rootTransform, slotIndex, contents);
                }

                EnqueueNewCollapse();

                voxelMesher.RegenerateMeshedVoxels(Map, Tileset.TileDimensions);
            }
        }
    }

    public void EnqueueNewCollapse()
    {
        if (isCurrentlyGenerating)
        {
            isGenerationQueued = true;
        }
        else
        {
            StartGeneration();
        }
    }

    private void StartGeneration()
    {
        isCurrentlyGenerating = true;

        onCollapseBeginning?.Invoke();

        NextGeneratingPlacer.voxelGrid = (VoxelGridAsset)workingMap.Clone();
        NextGeneratingPlacer.GenerateOverTime(OnGenerationCompleted);
    }

    private void OnGenerationCompleted(bool containsWildcards)
    {
        HidePlacerObjects(MostRecentlyGeneratedPlacer);
        mostRecentlyGeneratedPlacerIsFirst = !mostRecentlyGeneratedPlacerIsFirst;

        isCurrentlyGenerating = false;

        if (isGenerationQueued)
        {
            ReplaceSlotsChangedSinceGenerationStartedWithPlaceholders(MostRecentlyGeneratedPlacer);
            isGenerationQueued = false;
            StartGeneration();
        }
        else
        {
            // only propagate the generation complete if the user has stopped editing the mesh
            onGenerationComplete?.Invoke(containsWildcards);
        }
    }

    private static void HidePlacerObjects(GridPlacer placer)
    {
        placer.ClearChildren();
    }

    private void ReplaceSlotsChangedSinceGenerationStartedWithPlaceholders(GridPlacer targetPlacer)
    {
        var mapUsedInGeneration = targetPlacer.voxelGrid;
        var currentMap = Map;
        var placedModulesComponent = targetPlacer.transform.GetComponentInChildren<PlacedModulesRecord>();
        var rootTransform = placedModulesComponent.placedModules[0].instance.transform.parent;

        for (var moduleIndex = 0; moduleIndex < placedModulesComponent.placedModules.Length; ++moduleIndex)
        {
            var module = placedModulesComponent.placedModules[moduleIndex];

            var generatedContents = mapUsedInGeneration.GetCubeContents(module.sourceVoxels);
            var currentContents = currentMap.GetCubeContents(module.sourceVoxels);
            if (generatedContents != currentContents)
            {
                ReplaceSlotWithPlaceholder(placedModulesComponent.placedModules, rootTransform, moduleIndex, currentContents);
            }
        }
    }

    public void ReplaceAllSlotsWithPlaceholders()
    {
        var placer = MostRecentlyGeneratedPlacer;
        var currentMap = Map;
        var placedModulesComponent = placer.transform.GetComponentInChildren<PlacedModulesRecord>();
        var placedModules = placedModulesComponent.placedModules;
        var rootTransform = placedModules[0].instance.transform.parent;

        for (var i = 0; i < placedModules.Length; ++i)
        {
            var module = placedModules[i];
            var currentContents = currentMap.GetCubeContents(module.sourceVoxels);
            ReplaceSlotWithPlaceholder(placedModules, rootTransform, i, currentContents);
        }
    }

    private void ReplaceSlotWithPlaceholder(PlacedModulesRecord.PlacedModule[] placedModules, Transform rootTransform, int slotIndex, int voxelContents)
    {
        var placedModule = placedModules[slotIndex];
        if (placedModule.instance != null)
        {
            Destroy(placedModule.instance);
        }

        var slotData = cachedSlotGrid.SlotData[slotIndex];

        // we know the placeholders are always unflipped and unrotated, can use the slots directly
        var normalWarper = new NormalWarper(
            slotData.NormalX000, slotData.NormalY000, slotData.NormalZ000,
            slotData.NormalX001, slotData.NormalY001, slotData.NormalZ001,
            slotData.NormalX010, slotData.NormalY010, slotData.NormalZ010,
            slotData.NormalX011, slotData.NormalY011, slotData.NormalZ011,
            slotData.NormalX100, slotData.NormalY100, slotData.NormalZ100,
            slotData.NormalX101, slotData.NormalY101, slotData.NormalZ101,
            slotData.NormalX110, slotData.NormalY110, slotData.NormalZ110,
            slotData.NormalX111, slotData.NormalY111, slotData.NormalZ111
        );
        var sourceMesh = placeholderTiles.GetMeshObjectForCubeConfig(voxelContents);
        var newPlaceholderMesh = ModuleMeshInstantiator.InstantiateMeshAndCageWarp(sourceMesh,
                false,
                slotData.V000,
                slotData.V001,
                slotData.V010,
                slotData.V011,
                slotData.V100,
                slotData.V101,
                slotData.V110,
                slotData.V111,
                normalWarper
            );

        var newPlaceholder = new GameObject();
        newPlaceholder.transform.SetParent(rootTransform, worldPositionStays:false);
        newPlaceholder.transform.localPosition = Vector3.zero;
        newPlaceholder.transform.localRotation = Quaternion.identity;
        ModuleMeshInstantiator.AddMeshToObject(newPlaceholder, sourceMesh, newPlaceholderMesh);

        placedModule.instance = newPlaceholder;
    }

    public void SetTileset(Tileset tileset)
    {
        foreach (var placer in new[]{firstPlacer, secondPlacer})
        {
            placer.SetTileset(tileset);
        }
    }

    public delegate void OnCollapseEvent();
    public OnCollapseEvent onCollapseBeginning = null;
    public OnCollapseEvent onPerfectCollapseSucceeded = null;
    public OnCollapseEvent onPerfectCollapseFailed = null;
    public OnCollapseEvent onFoundCollapsableState = null;
    public OnCollapseEvent onRemovedRedundantWildcards = null;

    // only used for getting the module warpers
    private SlotGrid cachedSlotGrid;

    private void SetUpCollapseEventPropagators()
    {
        SetUpCollapseEventPropagatorsForSingleGridPlacer(firstPlacer);
        SetUpCollapseEventPropagatorsForSingleGridPlacer(secondPlacer);
    }

    private void SetUpCollapseEventPropagatorsForSingleGridPlacer(GridPlacer placer)
    {
        placer.onPerfectCollapseSucceeded += () => onPerfectCollapseSucceeded?.Invoke();
        placer.onPerfectCollapseFailed += () => onPerfectCollapseFailed?.Invoke();
        placer.onFoundCollapsableState += () => onFoundCollapsableState?.Invoke();
        placer.onRemovedRedundantWildcards += () => onRemovedRedundantWildcards?.Invoke();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This Unity component manages the grid placing during edit time and runtime.
///
/// It will automatically generate a mesh when the world starts, both in editor and in the game. It can also generate the mesh later by calling <see cref="GenerateNow"/>.
/// </summary>
[ExecuteAlways]
[SelectionBase]
[AddComponentMenu(MenuConstants.topMenuName+"Grid Placer", order:MenuConstants.addComponentOrderBase+0)]
public class GridPlacer : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs("marchingCubes")]
    [Tooltip("The asset containing the modules and tiles that GridPlacer will place over the map.")]
    private Tileset tileset = null;
    /// <summary>
    /// Readonly access to the tileset that is used by this GridPlacer instance.
    /// </summary>
    public Tileset Tileset => tileset;

    /// <summary>
    /// Set this GridPlacer's tileset and prepare any internal state required when tilesets change.
    /// </summary>
    public void SetTileset(Tileset tileset)
    {
        this.tileset = tileset;
    }

    /// <summary>
    /// This GridPlacer's map of voxels. It contains a 3D grid of voxels, and the GridPlacer will attempt to place modules around the borders between the filled and empty tiles.
    /// </summary>
    [SerializeField]
	[FormerlySerializedAs("map")]
    [Tooltip("The voxel map that GridPlacer will recreate using the tileset.")]
    public VoxelGridAsset voxelGrid = null;

    [SerializeField]
    [Tooltip("An asset containing wildcard backup tiles. GridPlacer will fall back to these tiles if it cannot place the tiles from the tileset.")]
    private DualGridVoxelMeshBackupTiles backupTiles = null;

    /// <summary>
    /// Changing the seed may cause the GridPlacer to place different tiles across the map. Tilesets must have multiple options for this to have any effect.
    ///
    /// A GridPlacer with the same map, tileset, and seed will always place modules in the same way.
    /// </summary>
    [SerializeField]
    public int seed = 0;

    /// <summary>
    /// When using <see cref="GenerateOverTime"/> in game, GridPlacer will attempt to run the generation for this number of seconds each frame.
    /// 
    /// GridPlacer will run its placement algorithm for at least this long before relinquishing control to other systems in Unity. GridPlacer will always take slightly longer than the specified amount.
    ///
    /// If you are running a framerate critical game then you can reduce this budget to ensure that the framerate always stays above your target.
    /// 
    /// Conversely, raising the budget will reduce the real-time delay before the map is available. Unity will execute fewer gameloops, so more CPU time will be focused on GridPlacer.
    ///
    /// This value is ignored whilst in editor - the value from your "Preferences &#8594; Wildtile &#8594; Maximum GridPlacer Seconds Per Frame In Editor" is used instead.
    /// </summary>
    [SerializeField]
    [Range(0f, 0.5f)]
    [Tooltip("When using GenerateOverTime() in game, GridPlacer will attempt to run the generation for this number of seconds each frame.\n\nThis value is ignored whilst in editor - the value from your \"Preferences -> Wildtile -> Maximum GridPlacer Seconds Per Frame In Editor\" is used instead.")]
    public float maxInGameSecondsPerFrame = 0.005f;

    /// <summary>
    /// How many wave function collapse steps will be performed before Wildtile believes it is in an infinite loop and stops the process.
    ///
    /// You may need to increase this for large maps and/or large tilesets.
    /// </summary>
    [SerializeField]
    [Tooltip("How many wave function collapse steps will be performed before Wildtile believes it is in an infinite loop and stops the process.\n" +
    "\n" + 
    "You may need to increase this for large maps and/or large tilesets.")]
    public int maxCollapseSteps = 10_000_000;

    /// <summary>
    /// How many object placement/welding steps will be performed before Wildtile believes it is in an infinite loop and stops the process.
    ///
    /// You may need to increase this for large maps and/or highly detailed tilesets.
    /// </summary>
    [SerializeField]
    [Tooltip("How many wave function collapse steps will be performed before Wildtile believes it is in an infinite loop and stops the process.\n" +
    "\n" + 
    "You may need to increase this for large maps and/or highly detailed tilesets.")]
    public int maxPostprocessSteps = 1_000_000;

    /// <summary>
    /// Show generated level objects in the hierarchy view. All edits will be discarded when starting the game or changing scenes regardless of whether this option is checked or not.
    /// </summary>
    [SerializeField]
    [Tooltip("Show generated level objects in the hierarchy view. All edits will be discarded when starting the game or changing scenes regardless of whether this option is checked or not.")]
    public bool showObjectsInHierarchy = false;

    /// <summary>
    /// Setting for whether a GridPlacer component should place modules when the component starts up (e.g. when Unity opens a scene containing a GridPlacer)
    /// </summary>
    public enum GenerateOnStartUp
    {
        /// <summary>
        /// Whenever Unity loads a GridPlacer, the GridPlacer will place modules in the tilemap.
        /// </summary>
        Always,

        /// <summary>
        /// When Unity loads a GridPlacer in edit-mode only, the GridPlacer will place modules in the tilemap.
        /// GridPlacer will not automatically place modules when loaded in game.
        ///
        /// Use <see cref="GenerateNow"/> or <see cref="GenerateOverTime"/> to make GridPlacer place modules.
        /// </summary>
        EditorOnly,

        /// <summary>
        /// GridPlacer will never automatically place modules when loaded in game.
        ///
        /// Use <see cref="GenerateNow"/> or <see cref="GenerateOverTime"/> to make GridPlacer place modules.
        /// </summary>
        Never,
    }

    /// <summary>
    /// In which environments should this component immediately generate when it spawns? If disabled, other code must trigger GridPlacer to build.
    /// </summary>
    [SerializeField]
    [Tooltip("In which environments should this component immediately generate when it spawns? If disabled, other code must trigger GridPlacer to build.")]
    public GenerateOnStartUp generateOnStartUp = GenerateOnStartUp.Always;

    /// <summary>
    /// Called during collapse to report information to the calling code at different points in the collapse, for example <see cref="onPerfectCollapseSucceeded"/> or <see cref="onPerfectCollapseFailed"/>.
    /// </summary>
    public delegate void OnCollapseEvent();

    /// <summary>
    /// Called when the collapse algorithm completes without adding any new wildcards.
    /// </summary>
    public OnCollapseEvent onPerfectCollapseSucceeded = null;
    /// <summary>
    /// Called when the collapse algorithm finds that it cannot collapse the initial state without adding wildcards.
    /// </summary>
    public OnCollapseEvent onPerfectCollapseFailed = null;
    /// <summary>
    /// Called when the collapse algorithm has added wildcards and found a collapsable state.
    /// It will now attempt to remove redundant wildcards.
    ///
    /// This will only be called if the perfect collapse fails, which would be reported through <see cref="onPerfectCollapseFailed"/>.
    /// </summary>
    public OnCollapseEvent onFoundCollapsableState = null;
    /// <summary>
    /// Called when the collapse algorithm has removed wildcards and found a minimal-wildcard collapsable state.
    ///
    /// This will only be called if the perfect collapse fails, which would be reported through <see cref="onPerfectCollapseFailed"/>.
    /// 
    /// This will be called after <see cref="onFoundCollapsableState"/>.
    /// </summary>
    public OnCollapseEvent onRemovedRedundantWildcards = null;

    private void Start()
    {
        if (generateOnStartUp == GenerateOnStartUp.Always)
        {
            GenerateNow();
        }
#if UNITY_EDITOR
        else if (generateOnStartUp == GenerateOnStartUp.EditorOnly && !UnityEngine.Application.IsPlaying(gameObject))
        {
            GenerateNow();
        }
#endif
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateChildHierarchyVisibility();
    }

    private void UpdateChildHierarchyVisibility()
    {
        foreach (Transform child in transform)
        {
            HideGeneratedObjects(child.gameObject);
        }
    }
#endif

    /// <summary>
    /// Generates a new mesh of modules using the associated voxelGrid and tileset assets, and returns once the mesh is fully generated.
    /// </summary>
    /// <param name="clearExisting">If true, clear all of this transform's children before generating the new mesh from the assets.</param>
    public void GenerateNow(bool clearExisting = true)
    {
        if (!HasResourcesToGenerate())
        {
            return;
        }

        generateNowPerfMarker.Begin();
        if (clearExisting)
        {
            ClearChildren();
        }

        foreach (var yieldResult in StartGenerationCoroutine(null));

        generateNowPerfMarker.End();
    }

    /// <summary>
    /// This delegate is called when <see cref="GenerateOverTime"/> completes generation.
    /// </summary>
    public delegate void OnGenerationComplete(bool containedWildcards);

    /// <summary>
    /// Starts generateing a new mesh of modules using the associated voxelGrid and tileset assets over time.
    /// </summary>
    /// <param name="onGenerationComplete">Called when the coroutine finishes creating the transformation of the voxel grid into models</param>
    public void GenerateOverTime(OnGenerationComplete onGenerationComplete = null)
    {
        if (!HasResourcesToGenerate())
        {
            return;
        }

        ClearChildren();

        generatorCoroutine = StartGenerationCoroutine(onGenerationComplete).GetEnumerator();
    }

    /// <summary>
    /// Stop the generation that's currently in progress if there was one. This is silently ignored if there was no generation started, or if the generation has already completed.
    /// </summary>
    public void InterruptMeshGeneration()
    {
        generatorCoroutine = null;
    }

    private IEnumerable<CoroutineStatus> StartGenerationCoroutine(OnGenerationComplete onGenerationComplete)
    {
        var gridBuilder = new GridBuilderVisitor(tileset);
        voxelGrid.Visit(gridBuilder);
        var slotGrid = gridBuilder.slotGrid;
        
        var collapser = tileset.CreateWaveFunctionCollapser(seed);

        // propagate slot grid
        var iterationBounder = 0;
        foreach (var collapseYieldResult in collapser.InitialPropagateAndCollapseWithWildcardRetreatAndAdvance(slotGrid))
        {
            if (iterationBounder++ > maxCollapseSteps)
            {
                Debug.LogWarning("Collapse took too long, breaking out");
                break;
            }

            switch (collapseYieldResult)
            {
            case CoroutineStatus.PerfectCollapseSucceeded:
                onPerfectCollapseSucceeded?.Invoke();
                break;
            case CoroutineStatus.PerfectCollapseFailed:
                onPerfectCollapseFailed?.Invoke();
                break;
            case CoroutineStatus.FoundCollapsableState:
                onFoundCollapsableState?.Invoke();
                break;
            case CoroutineStatus.RemovedRedundantWildcards:
                onRemovedRedundantWildcards?.Invoke();
                break;
            default:
                break;
            }

            yield return collapseYieldResult;
        }

        // use the postprocessor to turn the grid into in-game objects
        var spawnRoot = new GameObject("Wildtile Root");
        HideGeneratedObjects(spawnRoot);
        spawnRoot.transform.SetParent(this.transform);
        spawnRoot.transform.localPosition = gridBuilder.RootPosition;
        spawnRoot.transform.localRotation = Quaternion.identity;
        spawnRoot.transform.localScale = Vector3.one;

        var postprocessableMapReport = PreparePostprocessableMap(slotGrid);
        var postprocessableMap = postprocessableMapReport.postprocessableMap;

        iterationBounder = 0;
        foreach (var postprocessorYieldResult in tileset.Postprocessor.Postprocess(spawnRoot, postprocessableMap, tileset.TileDimensions))
        {
            if (iterationBounder++ > maxPostprocessSteps)
            {
                Debug.LogWarning("Postprocess took too long, breaking out");
                break;
            }

            yield return CoroutineStatus.Postprocessing;
        }

        onGenerationComplete?.Invoke(postprocessableMapReport.containsWildcards);
    }

    private bool HasResourcesToGenerate()
    {
        return voxelGrid != null && tileset != null;
    }

    /// <summary>
    /// Destroy children of this GridPlacer's Transform.
    ///
    /// GridPlacer creates modules as children of its Transform, so in normal operation this will destroy a GridPlacer-generated map.
    /// If other scripts modify the hierarchy (move GridPlacer generated objects out, add non-GridPlacer child objects) then this may have unintended behaviour.
    /// </summary>
    public void ClearChildren()
    {
#if UNITY_EDITOR
        if (Application.IsPlaying(this))
        {
            ClearChildrenInPlayer();
        }
        else
        {
            ClearChildrenInEditMode();
        }
#else
        ClearChildrenInPlayer();
#endif
    }

    private void HideGeneratedObjects(GameObject root)
    {
        // only hide the root. All sub objects are not saved, but are still in
        // the scene and the user can click on them to select this GridPlacer
        // object, which is a nicer user experience.
        root.hideFlags = showObjectsInHierarchy ? shownSpawnedObjectFlags : hiddenSpawnedObjectFlags;
    }

    private void ClearChildrenInEditMode()
    {
        while (transform.childCount != 0)
        {
            var childTransform = transform.GetChild(0);
            childTransform.SetParent(null);
            GameObject.DestroyImmediate(childTransform.gameObject);
        }
    }

    private void ClearChildrenInPlayer()
    {
        while (transform.childCount != 0)
        {
            var childTransform = transform.GetChild(0);
            childTransform.SetParent(null);
            GameObject.Destroy(childTransform.gameObject);
        }
    }

    private struct PostprocessableMapReport
    {
        public Postprocessing.PostprocessableMap postprocessableMap;
        public bool containsWildcards;
    }

    private PostprocessableMapReport PreparePostprocessableMap(SlotGrid slotGrid)
    {
        var result = new Postprocessing.PostprocessableMap(slotGrid.SlotData.Length);
        var containsWildcards = false;

        var slotToPostprocessableSlot = new Dictionary<Slot, Postprocessing.PostprocessableMap.Slot>();

        var inverseTileWidth = 1f / tileset.TileDimensions.x;
        var inverseTileHeight = 1f / tileset.TileDimensions.y;

        for (var i = 0; i < slotGrid.SlotData.Length; ++i)
        {
            var slot = slotGrid.SlotData[i];
            if (slot.isWildcard)
            {
                containsWildcards = true;

                var cubeConfig = slot.contentsIndex;
                var backupMesh = backupTiles.GetMeshObjectForCubeConfig(cubeConfig);

                var newPostprocessableSlot = new Postprocessing.PostprocessableMap.Slot{
                    prefab = null,
                    mesh = backupMesh,
                    flipIndices = false,
                    sourceVoxels = slot.sourceVoxels,

                    v000 = Vector3.Scale(slot.V000, tileset.TileDimensions),
                    v001 = Vector3.Scale(slot.V001, tileset.TileDimensions),
                    v010 = Vector3.Scale(slot.V010, tileset.TileDimensions),
                    v011 = Vector3.Scale(slot.V011, tileset.TileDimensions),
                    v100 = Vector3.Scale(slot.V100, tileset.TileDimensions),
                    v101 = Vector3.Scale(slot.V101, tileset.TileDimensions),
                    v110 = Vector3.Scale(slot.V110, tileset.TileDimensions),
                    v111 = Vector3.Scale(slot.V111, tileset.TileDimensions),
                    normalWarper = new NormalWarper(
                        slot.NormalX000 * inverseTileWidth, slot.NormalY000 * inverseTileHeight, slot.NormalZ000 * inverseTileWidth,
                        slot.NormalX001 * inverseTileWidth, slot.NormalY001 * inverseTileHeight, slot.NormalZ001 * inverseTileWidth,
                        slot.NormalX010 * inverseTileWidth, slot.NormalY010 * inverseTileHeight, slot.NormalZ010 * inverseTileWidth,
                        slot.NormalX011 * inverseTileWidth, slot.NormalY011 * inverseTileHeight, slot.NormalZ011 * inverseTileWidth,
                        slot.NormalX100 * inverseTileWidth, slot.NormalY100 * inverseTileHeight, slot.NormalZ100 * inverseTileWidth,
                        slot.NormalX101 * inverseTileWidth, slot.NormalY101 * inverseTileHeight, slot.NormalZ101 * inverseTileWidth,
                        slot.NormalX110 * inverseTileWidth, slot.NormalY110 * inverseTileHeight, slot.NormalZ110 * inverseTileWidth,
                        slot.NormalX111 * inverseTileWidth, slot.NormalY111 * inverseTileHeight, slot.NormalZ111 * inverseTileWidth
                    )
                };

                slotToPostprocessableSlot.Add(slot, newPostprocessableSlot);
                result.slots[i] = newPostprocessableSlot;
            }
            else
            {
                var newPostprocessableSlot = GetPostprocessableSlotForSlot(slot, tileset.Modules);
                slotToPostprocessableSlot.Add(slot, newPostprocessableSlot);
                result.slots[i] = newPostprocessableSlot;
            }
        }

        foreach (var slot in slotGrid.SlotData)
        {
            var correspondingPostprocessableSlot = slotToPostprocessableSlot[slot];
            foreach (var face in FaceDataSerialization.serializationFaces)
            {
                var originalHalfLoop = slot.halfLoops[face.face];
                if (originalHalfLoop != null)
                {
                    Assert.IsNotNull(originalHalfLoop.targetSlot);
                    var postprocessableHalfLoop = new Postprocessing.PostprocessableMap.SlotHalfLoop();
                    postprocessableHalfLoop.targetSlotIndex = originalHalfLoop.targetSlotIndex;
                    postprocessableHalfLoop.targetSlot = slotToPostprocessableSlot[originalHalfLoop.targetSlot];
                    postprocessableHalfLoop.facingFaceOnTarget = originalHalfLoop.facingFaceOnTarget;
                    correspondingPostprocessableSlot.halfLoops[face.face] = postprocessableHalfLoop;
                }
                else
                {
                    correspondingPostprocessableSlot.halfLoops[face.face] = new Postprocessing.PostprocessableMap.SlotHalfLoop{
                        targetSlotIndex = -1,
                        targetSlot = null,
                        // facingFaceOnTarget is unset and should not be used
                    };
                }
            }
        }

        return new PostprocessableMapReport{
            postprocessableMap = result,
            containsWildcards = containsWildcards,
        };
    }

    private const HideFlags shownSpawnedObjectFlags = HideFlags.DontSave;
    private const HideFlags hiddenSpawnedObjectFlags = HideFlags.HideAndDontSave | HideFlags.NotEditable;

    private Postprocessing.PostprocessableMap.Slot GetPostprocessableSlotForSlot(Slot slot, IList<Module> modules)
    {
        if (slot.AvailableModules.Count() == 0)
        {
            Debug.LogError($"Slot {slot.Position} didn't have any available modules");
            return new Postprocessing.PostprocessableMap.Slot{
                prefab = null,
                mesh = null,
                flipIndices = false,
                yawIndex = 0,
                sourceVoxels = slot.sourceVoxels,

                v000 = Vector3.Scale(slot.V000, tileset.TileDimensions),
                v001 = Vector3.Scale(slot.V001, tileset.TileDimensions),
                v010 = Vector3.Scale(slot.V010, tileset.TileDimensions),
                v011 = Vector3.Scale(slot.V011, tileset.TileDimensions),
                v100 = Vector3.Scale(slot.V100, tileset.TileDimensions),
                v101 = Vector3.Scale(slot.V101, tileset.TileDimensions),
                v110 = Vector3.Scale(slot.V110, tileset.TileDimensions),
                v111 = Vector3.Scale(slot.V111, tileset.TileDimensions),
            };
        }

        var transformedModule = slot.AvailableModules.First();
        var module = modules[transformedModule.moduleIndex];

        var vertexTransformer = new CubeVertexTransformer(
            slot.V000, slot.NormalX000, slot.NormalY000, slot.NormalZ000,
            slot.V001, slot.NormalX001, slot.NormalY001, slot.NormalZ001,
            slot.V010, slot.NormalX010, slot.NormalY010, slot.NormalZ010,
            slot.V011, slot.NormalX011, slot.NormalY011, slot.NormalZ011,
            slot.V100, slot.NormalX100, slot.NormalY100, slot.NormalZ100,
            slot.V101, slot.NormalX101, slot.NormalY101, slot.NormalZ101,
            slot.V110, slot.NormalX110, slot.NormalY110, slot.NormalZ110,
            slot.V111, slot.NormalX111, slot.NormalY111, slot.NormalZ111
        );

        vertexTransformer.RotateVertices(transformedModule.yawIndex);
        if (transformedModule.isFlipped)
        {
            vertexTransformer.FlipVertices();
        }

        return new Postprocessing.PostprocessableMap.Slot{
            prefab = module.prefab,
            mesh = module.mesh,
            flipIndices = transformedModule.isFlipped,
            yawIndex = transformedModule.yawIndex,
            moduleName = $"Wildtile Mesh Instance ({module.name} yaw {transformedModule.yawIndex * 90}{(transformedModule.isFlipped ? " (flipped)" : "")})",
            sourceVoxels = slot.sourceVoxels,

            v000 = Vector3.Scale(vertexTransformer.V000, tileset.TileDimensions),
            v001 = Vector3.Scale(vertexTransformer.V001, tileset.TileDimensions),
            v010 = Vector3.Scale(vertexTransformer.V010, tileset.TileDimensions),
            v011 = Vector3.Scale(vertexTransformer.V011, tileset.TileDimensions),
            v100 = Vector3.Scale(vertexTransformer.V100, tileset.TileDimensions),
            v101 = Vector3.Scale(vertexTransformer.V101, tileset.TileDimensions),
            v110 = Vector3.Scale(vertexTransformer.V110, tileset.TileDimensions),
            v111 = Vector3.Scale(vertexTransformer.V111, tileset.TileDimensions),

            normalWarper = new NormalWarper(
                vertexTransformer.NormalX000, vertexTransformer.NormalY000, vertexTransformer.NormalZ000,
                vertexTransformer.NormalX001, vertexTransformer.NormalY001, vertexTransformer.NormalZ001,
                vertexTransformer.NormalX010, vertexTransformer.NormalY010, vertexTransformer.NormalZ010,
                vertexTransformer.NormalX011, vertexTransformer.NormalY011, vertexTransformer.NormalZ011,
                vertexTransformer.NormalX100, vertexTransformer.NormalY100, vertexTransformer.NormalZ100,
                vertexTransformer.NormalX101, vertexTransformer.NormalY101, vertexTransformer.NormalZ101,
                vertexTransformer.NormalX110, vertexTransformer.NormalY110, vertexTransformer.NormalZ110,
                vertexTransformer.NormalX111, vertexTransformer.NormalY111, vertexTransformer.NormalZ111
            ),
        };
    }

#if UNITY_EDITOR
    private void OnEnable()
    {
        UnityEditor.EditorApplication.update += EditorUpdate;
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    private void OnDisable()
    {
        UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
        UnityEditor.EditorApplication.update -= EditorUpdate;
    }

    private void EditorUpdate()
    {
        if (!UnityEngine.Application.IsPlaying(gameObject))
        {
            RunTimeSlicedGeneratorCoroutine();
        }
    }
#endif

    private void Update()
    {
#if UNITY_EDITOR
        if (!UnityEngine.Application.IsPlaying(gameObject))
        {
            // handled by EditorUpdate()
            return;
        }
#endif
        RunTimeSlicedGeneratorCoroutine();
    }

    private void RunTimeSlicedGeneratorCoroutine()
    {
        if (generatorCoroutine != null)
        {
            try
            {
                if (RunCoroutineForTimeSlice(generatorCoroutine, GetMaxSecondsPerFrame()) == CoroutineRunningStatus.Completed)
                {
                    generatorCoroutine = null;
                }
            }
            catch (Exception)
            {
                generatorCoroutine = null;
                throw;
            }
        }
    }

#if UNITY_EDITOR
    private void OnBeforeAssemblyReload()
    {
        RunCoroutineToCompletion();
    }
#endif

    private void RunCoroutineToCompletion()
    {
        if (generatorCoroutine != null)
        {
            try
            {
                while (generatorCoroutine.MoveNext());
            }
            finally
            {
                generatorCoroutine = null;
            }
        }
    }

    private float GetMaxSecondsPerFrame()
    {
#if UNITY_EDITOR
        if (UnityEngine.Application.IsPlaying(gameObject))
        {
            return maxInGameSecondsPerFrame;
        }
        else
        {
            Assert.IsNotNull(getEditorMaxSecondsPerFrame);
            return getEditorMaxSecondsPerFrame();
        }
#else
        return maxInGameSecondsPerFrame;
#endif
    }

    private enum CoroutineRunningStatus
    {
        Running,
        Completed,
    }
    private static CoroutineRunningStatus RunCoroutineForTimeSlice(IEnumerator<CoroutineStatus> coroutine, float maxTimeInSeconds)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();

        while (coroutine.MoveNext())
        {
            if (watch.ElapsedMilliseconds > maxTimeInSeconds * 1000f)
            {
                return CoroutineRunningStatus.Running;
            }
        }

        return CoroutineRunningStatus.Completed;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Injectable settings getter that grabs the current max seconds per frame in editor only.
    /// </summary>
    public delegate float GetEditorMaxSecondsPerFrame();

    /// <summary>
    /// Injectable settings getter instance that grabs the current max seconds per frame in editor only.
    ///
    /// This value is ignored in play mode and in built players
    /// </summary>
    public static GetEditorMaxSecondsPerFrame getEditorMaxSecondsPerFrame = null;
#endif

    private static readonly ProfilerMarker generateNowPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "GridPlacer.GenerateNow");

    private IEnumerator<CoroutineStatus> generatorCoroutine = null;
    /// <summary>
    /// Returns true if this GridPlacer is currently at any stage of placing modules (e.g. collapsing the wave function, warping or welding models).
    /// </summary>
    public bool IsGenerating => generatorCoroutine != null;
}

}

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.EditorTools;
using Unity.Profiling;

using CarbideFunction.Wildtile;

using PlacerComponent = CarbideFunction.Wildtile.GridPlacer;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class is a <see href="https://docs.unity3d.com/ScriptReference/EditorTools.EditorTool.html">Unity Tool</see> that allows the user to visually edit <see cref="GridPlacer"/> maps. It affects the data in <see cref="VoxelGrid"/>.
///
/// Opening this tool will replace the selected GridPlacer's models with a simple voxel grid that can be edited by the user. <see href="~/articles/editing_grids.md">See the manual pages for information on how to use the tool.</see>
/// </summary>
[EditorTool("Carbide Function/Tile Map Editor", typeof(PlacerComponent))]
internal class TileMapEditorTool : EditorTool
{
    [SerializeField]
    private Material editorMaterial = null;
    [SerializeField]
    private Material editorWallsMaterial = null;
    [SerializeField]
    private Material editorVoxelTouchingWallsMaterial = null;

    [SerializeField]
    private Material processingEditorMaterial = null;
    [SerializeField]
    private Material processingEditorWallsMaterial = null;
    [SerializeField]
    private Material processingEditorVoxelTouchingWallsMaterial = null;

    [SerializeField]
    [Layer]
    private int voxelGridLayer = 0;

    private GUIContent iconContent;
    private VoxelMesher voxelMesher;

    private static TileMapEditorTool toolInstance = null;

    private void OnEnable()
    {
        Assert.IsNull(toolInstance);
        toolInstance = this;

        iconContent = new GUIContent{
            text = "Tile Map Editor",
            tooltip = String.Format("Carbide Function tile voxelGrid editor, for use on {0} components", typeof(PlacerComponent).Name)
        };

        voxelMesher = new VoxelMesher();
    }

    public override GUIContent toolbarIcon => iconContent;

    /// <summary>
    /// Opens the tool if the editor is currently able to do so.
    ///
    /// Calls to this method will be silently ignored if the user hasn't selected a <see cref="GridPlacer"/>, or if they have selected more than one object.
    /// </summary>
    [Shortcut("Tools/Voxel Editor", context:null, KeyCode.U)]
    public static void OpenTool()
    {
        if (   (toolInstance?.IsAvailable() ?? false)
            && IsSelectingEditableObject()
        )
        {
            ToolManager.SetActiveTool(toolInstance);
        }
    }

    private static bool IsSelectingEditableObject()
    {
        // Even if selection.count == 1, activeTransform could be null if the selection contained something
        // else that doesn't have a transform e.g. an asset.
        if (Selection.count == 1 && Selection.activeTransform != null)
        {
            if (Selection.activeTransform.gameObject.GetComponent<PlacerComponent>() != null)
            {
                return true;
            }
        }
        return false;
    }

    public override void OnActivated()
    {
        HideTargetContents();
        tileMapEditorToolPerfMarker.Begin();
        voxelMesher.DestroyMeshedVoxels();
        CastTarget.InterruptMeshGeneration();
        voxelMesher.CreateMeshedVoxels(CastTarget.voxelGrid, CastTarget.Tileset.TileDimensions, editorMaterial, editorWallsMaterial, editorVoxelTouchingWallsMaterial, CastTarget.transform, voxelGridLayer);
        tileMapEditorToolPerfMarker.End();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnUndoRedo()
    {
        tileMapEditorToolPerfMarker.Begin();
        undoRedoPerfMarker.Begin();
        voxelMesher.RegenerateMeshedVoxels(CastTarget.voxelGrid, CastTarget.Tileset.TileDimensions);
        undoRedoPerfMarker.End();
        tileMapEditorToolPerfMarker.End();
    }

    public override void OnToolGUI(EditorWindow window)
    {
        tileMapEditorToolPerfMarker.Begin();
        var evt = Event.current;

        if (evt.type == EventType.MouseDown)
        {
            if (evt.button == 0)
            {
                Click(evt);
                evt.Use();
            }
        }

        // prevent the user from accidentally clicking off this tool
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        window.Repaint();
        tileMapEditorToolPerfMarker.End();
    }

    private Vector3 GetCurrentMousePositionInScene()
    {
        var voxelHit = GetHitUnderCursor();
        return voxelHit?.point ?? HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(10f);
    }

    private RaycastHit? GetHitUnderCursor()
    {
        var mousePosition = Event.current.mousePosition;
        var hits = Physics.RaycastAll(HandleUtility.GUIPointToWorldRay(mousePosition));
        var voxelHit = hits.Select((hit, index) => new {hit, index}).FirstOrDefault(hit => hit.hit.collider.transform == voxelMesher.TempMeshInstance.transform);
        return voxelHit?.hit;
    }

    private const string editingGridUndoName = "Editing Wildtile Grid";

    private void Click(Event evt)
    {
        clickPerfMarker.Begin();
        var hit = GetHitUnderCursor();
        if (hit.HasValue)
        {
            var faceData = voxelMesher.GetFaceDataForTriIndex(hit.Value.triangleIndex);
            // need the IVoxelGrid so I can access the interface-implemented method
            var voxelGrid = (IVoxelGrid)CastTarget.voxelGrid;

            if (evt.control)
            {
                if (faceData.voxelIndex != IVoxelGrid.outOfBoundsVoxelIndex)
                {
                    SetVoxelContents(voxelGrid, faceData.voxelIndex, 0);
                }
            }
            else
            {
                if (faceData.facingVoxelIndex != IVoxelGrid.outOfBoundsVoxelIndex)
                {
                    SetVoxelContents(voxelGrid, faceData.facingVoxelIndex, 1);
                }
            }

            voxelMesher.RegenerateMeshedVoxels(CastTarget.voxelGrid, CastTarget.Tileset.TileDimensions);
        }
        clickPerfMarker.End();
    }

    private static void SetVoxelContents
    (
        IVoxelGrid voxelGrid,
        int index,
        int newContents
    )
    {
        Assert.IsNotNull(voxelGrid);
        voxelGrid.Visit(new SetVoxelContentsVisitor(index, newContents));
    }

    private class SetVoxelContentsVisitor : IVoxelGridVisitor
    {
        public SetVoxelContentsVisitor
        (
            int voxelIndex,
            int newContents
        )
        {
            this.voxelIndex = voxelIndex;
            this.newContents = newContents;
        }

        int voxelIndex;
        int newContents;

        public void VisitRectangularVoxelGrid(VoxelGrid grid)
        {
            Undo.RecordObject(grid, editingGridUndoName);
            var serializedGrid = new SerializedObject(grid);
            Assert.IsNotNull(serializedGrid);

            serializedGrid.FindProperty(VoxelGrid.voxelDataName).GetArrayElementAtIndex(voxelIndex).intValue = newContents;

            serializedGrid.ApplyModifiedProperties();
        }

        public void VisitIrregularVoxelGrid(IrregularVoxelGrid grid)
        {
            Undo.RecordObject(grid, editingGridUndoName);
            var serializedGrid = new SerializedObject(grid);
            Assert.IsNotNull(serializedGrid);

            serializedGrid.FindProperty(nameof(IrregularVoxelGrid.voxelContents)).GetArrayElementAtIndex(voxelIndex).intValue = newContents;

            serializedGrid.ApplyModifiedProperties();
        }
    }

    public override void OnWillBeDeactivated()
    {
        tileMapEditorToolPerfMarker.Begin();
        Undo.undoRedoPerformed -= OnUndoRedo;

        voxelMesher.ChangeMaterials(processingEditorMaterial, processingEditorWallsMaterial, processingEditorVoxelTouchingWallsMaterial);

        if (CastTarget != null)
        {
            ShowTargetContents();
        }
        tileMapEditorToolPerfMarker.End();
    }

    private void OnGenerationComplete(bool containsWildcards)
    {
        LateDeleteEditorMesh();
    }

    private void LateDeleteEditorMesh()
    {
        voxelMesher.DestroyMeshedVoxels();
    }

    private void HideTargetContents()
    {
        DoOnTargetChildren(child => child.gameObject.SetActive(false));
    }

    private void DoOnTargetChildren(Action<GameObject> action)
    {
        var childrenCount = CastTarget.transform.childCount;

        foreach (var child in Enumerable.Range(0, childrenCount).Select(childIndex => CastTarget.transform.GetChild(childIndex)))
        {
            action(child.gameObject);
        }
    }

    private void ShowTargetContents()
    {
        DoOnTargetChildren(child => child.gameObject.SetActive(true));
        CastTarget.GenerateOverTime(OnGenerationComplete);
    }

    private PlacerComponent CastTarget => target as PlacerComponent;

    private static readonly ProfilerMarker tileMapEditorToolPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.Update");
    private static readonly ProfilerMarker clickPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.Click");
    private static readonly ProfilerMarker undoRedoPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "TileMapEditorTool.UndoRedo");
}

}

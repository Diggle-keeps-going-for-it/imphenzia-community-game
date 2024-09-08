using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class OutOfBoundsDrawer : IDiagnosticsDrawer
{
    [Serializable]
    public class Data
    {
        private readonly static Lazy<GUIContent> vertexIndicatorSizeGui = new Lazy<GUIContent>(() =>
            new GUIContent(
                text: ObjectNames.NicifyVariableName(nameof(vertexIndicatorSize)),
                tooltip: "The size of the vertex indicator spheres. This is automatically scaled by the tile's bounds."
            ), isThreadSafe:false);
        public float vertexIndicatorSize = 0.05f;

        private readonly static Lazy<GUIContent> doubleClickVertexFramingSizeGui = new Lazy<GUIContent>(() =>
            new GUIContent(
                text: ObjectNames.NicifyVariableName(nameof(doubleClickVertexFramingSize)),
                tooltip: "The amount the camera should zoom out when double clicking on the vertices in the inspector. This is automatically scaled by the tile's bounds."
            ), isThreadSafe:false);
        public float doubleClickVertexFramingSize = 0.5f;

        private readonly static Lazy<GUIContent> highlightedVerticesCountGui = new Lazy<GUIContent>(() =>
            new GUIContent(
                text: ObjectNames.NicifyVariableName(nameof(highlightedVerticesCount)),
                tooltip: "How many vertices to show when highlighting vertices out of bounds. The drawing method used may cause poor performance when highlighting many vertices at once."
            ), isThreadSafe:false);
        public int highlightedVerticesCount = 100;

        public bool showTileBounds = true;

        [SerializeField]
        private bool isExpandedInOptionsMenu = true;

        public void OnGui()
        {
            isExpandedInOptionsMenu = EditorGUILayout.BeginFoldoutHeaderGroup(isExpandedInOptionsMenu, "Out Of Bounds Vertices Drawer");
            if (isExpandedInOptionsMenu)
            {
                using (var changeCheck = new EditorGUI.ChangeCheckScope())
                {
                    vertexIndicatorSize = EditorGUILayout.Slider(vertexIndicatorSizeGui.Value, vertexIndicatorSize, 0f, 1f);
                    doubleClickVertexFramingSize = EditorGUILayout.Slider(doubleClickVertexFramingSizeGui.Value, doubleClickVertexFramingSize, 0f, 1f);
                    highlightedVerticesCount = Math.Max(EditorGUILayout.IntField(highlightedVerticesCountGui.Value, highlightedVerticesCount), 0);
                    showTileBounds = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(nameof(showTileBounds)), showTileBounds);
                    if (changeCheck.changed)
                    {
                        RedrawSceneViews.Redraw();
                    }
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

    public OutOfBoundsDrawer(IList<Vector3> vertexPositions, IEnumerable<int> outOfBoundsVertexIndices, int selectedOutOfBoundsVerticesListIndex, TilesetImporterAsset importer, Data data)
    {
        this.outOfBoundsVertexPositions = GetOutOfBoundsVertexPositions(vertexPositions, outOfBoundsVertexIndices);
        this.selectedOutOfBoundsVerticesListIndex = selectedOutOfBoundsVerticesListIndex;
        this.importer = importer;
        this.smallestTileDimension = GetSmallestDimension(importer);
        this.data = data;
    }

    private static List<Vector3> GetOutOfBoundsVertexPositions(IList<Vector3> vertexPositions, IEnumerable<int> outOfBoundsVertexIndices)
    {
        return outOfBoundsVertexIndices.Select(index => vertexPositions[index]).ToList();
    }

    void IDiagnosticsDrawer.Draw(SceneView view)
    {
        if (AreAnyVerticesOutOfBounds())
        {
            var viewDir = view.camera.transform.forward;
            DrawVerticesOutsideOfBounds(viewDir);
        }
        else
        {
            DrawNoOutsideOfBounds();
        }

        if (data.showTileBounds)
        {
            DrawTileBounds();
        }
    }

    private bool AreAnyVerticesOutOfBounds()
    {
        return this.outOfBoundsVertexPositions.Count > 0;
    }

    private void DrawVerticesOutsideOfBounds(Vector3 viewDir)
    {
        // draw selected vertex second so it renders on top of the other vertices
        DrawUnselectedVertices(viewDir);
        DrawSelectedVertex(viewDir);
    }

    private void DrawUnselectedVertices(Vector3 viewDir)
    {
        var smallHalfHighlightedVertices = data.highlightedVerticesCount / 2;
        // round up the number if odd
        var largeHalfHighlightedVertices = smallHalfHighlightedVertices + data.highlightedVerticesCount % 2;
        var nearbyOutOfBoundsVertexIndicesStartIndex = Math.Max(selectedOutOfBoundsVerticesListIndex - largeHalfHighlightedVertices, 0);
        var nearbyOutOfBoundsVertexIndicesEndIndex   = Math.Min(selectedOutOfBoundsVerticesListIndex + smallHalfHighlightedVertices, outOfBoundsVertexPositions.Count);

        for (var index = nearbyOutOfBoundsVertexIndicesStartIndex; index < nearbyOutOfBoundsVertexIndicesEndIndex; ++index)
        {
            if (index != selectedOutOfBoundsVerticesListIndex)
            {
                DrawUnselectedVertex(index, viewDir);
            }
        }
    }

    private void DrawUnselectedVertex(int vertexIndex, Vector3 viewDir)
    {
        Handles.color = outsideBoundsColor;
        DrawVertexIndicator(vertexIndex, viewDir);
    }

    private void DrawSelectedVertex(Vector3 viewDir)
    {
        if (IsVertexSelected())
        {
            Handles.color = selectedColor;
            DrawVertexIndicator(selectedOutOfBoundsVerticesListIndex, viewDir);
        }
    }

    private bool IsVertexSelected()
    {
        return selectedOutOfBoundsVerticesListIndex >= 0;
    }

    private void DrawVertexIndicator(int vertexIndex, Vector3 viewDir)
    {
        Assert.IsTrue(vertexIndex >= 0);
        Assert.IsTrue(vertexIndex < outOfBoundsVertexPositions.Count);

        var vertexPosition = outOfBoundsVertexPositions[vertexIndex];
        var indicatorSizeScaledByTile = data.vertexIndicatorSize * smallestTileDimension;

        Handles.DrawWireDisc(vertexPosition, viewDir, indicatorSizeScaledByTile, 0f);
    }

    private void DrawNoOutsideOfBounds()
    {
        DrawAllVerticesValidLabel();
    }

    private void DrawAllVerticesValidLabel()
    {
    }

    void IDiagnosticsDrawer.Shutdown()
    {}

    private static float GetSmallestDimension(TilesetImporterAsset importer)
    {
        return Mathf.Min(importer.importerSettings.tileWidth, importer.importerSettings.tileHeight);
    }

    private void DrawTileBounds()
    {
        if (importer != null)
        {
            var bounds = new Vector3(importer.importerSettings.tileWidth, importer.importerSettings.tileHeight, importer.importerSettings.tileWidth);
            Handles.color = boundsColor;
            Handles.DrawWireCube(Vector3.zero, bounds);
        }
    }

    private static readonly Color outsideBoundsColor = Color.red;
    private static readonly Color selectedColor = Color.white;
    private static readonly Color boundsColor = Color.blue;

    private List<Vector3> outOfBoundsVertexPositions;
    private int selectedOutOfBoundsVerticesListIndex;
    private TilesetImporterAsset importer;
    private float smallestTileDimension;

    private Data data;
}

}

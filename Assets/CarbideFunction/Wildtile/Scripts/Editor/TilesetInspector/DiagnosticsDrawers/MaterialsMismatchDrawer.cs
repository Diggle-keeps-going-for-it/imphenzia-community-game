using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using CarbideFunction.Wildtile.Editor;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class MaterialsMismatchDrawer : ConnectionsDrawer
{
    public MaterialsMismatchDrawer(Vector3 worldDelta, Vector3 tileDimensions, 
        List<KeyValuePair<Edge, Edge>> matchingEdges,
        List<KeyValuePair<Edge, Edge>> invalidEdges,
        List<Edge> invalidOrphanedEdgesOnCurrent,
        List<Edge> invalidOrphanedEdgesOnConnected,
        FaceDefinition face
    )
        : base(worldDelta, tileDimensions)
    {
        this.matchingEdges = ScaleEdges(matchingEdges);
        this.invalidEdges = ScaleEdges(invalidEdges);
        this.invalidOrphanedEdgesOnCurrent = ScaleEdges(invalidOrphanedEdgesOnCurrent);
        this.invalidOrphanedEdgesOnConnected = ScaleEdges(invalidOrphanedEdgesOnConnected);
        this.face = face;
    }

    private static List<Edge> FirstWithMatchedOrderingWithSecond(IEnumerable<Edge> destinationEdges, IList<Edge> sourceEdges)
    {
        return destinationEdges.ToList();
    }

    protected override void DrawConnections(SceneView view)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
        var delta = face.gridOffset;
        var orientation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, Vector3.up)) * Quaternion.LookRotation(delta, Vector3.up);

        var lineThickness = UserSettings.instance.tilesetInspectorSettings.lineThickness;
        var offset = UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier - 1f;
        var dashLength = UserSettings.instance.tilesetInspectorSettings.dashedLineDashLength;

        var currentModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(delta, tileDimensions) * 0.5f, orientation, Vector3.one);
        var otherModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(delta, tileDimensions) * (0.5f + offset), orientation, Vector3.one);

        foreach (var matchingEdge in matchingEdges)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Key.start);
            var endPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Key.end);
            var otherStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Value.start);
            var otherEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Value.end);

            Handles.color = Color.green;
            Handles.DrawLine(
                startPoint,
                endPoint,
                lineThickness
            );

            Handles.DrawLine(
                otherEndPoint,
                otherStartPoint,
                lineThickness
            );

            CustomDrawing.DrawDottedLine(
                Vector3.Lerp(startPoint, endPoint, 0.5f),
                Vector3.Lerp(otherEndPoint, otherStartPoint, 0.5f),
                lineThickness,
                dashLength
            );

            // draw matching materials here if I change my mind
        }

        foreach (var nonMatchingEdge in invalidEdges)
        {
            var sourceEdge = nonMatchingEdge.Key;
            var destinationEdge = nonMatchingEdge.Value;

            var sourceStartPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)sourceEdge.start);
            var sourceEndPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)sourceEdge.end);

            var destinationStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)destinationEdge.start);
            var destinationEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)destinationEdge.end);

            Handles.color = Color.red;
            Handles.DrawLine(
                sourceStartPoint,
                sourceEndPoint,
                lineThickness
            );
            Handles.DrawLine(
                destinationStartPoint,
                destinationEndPoint,
                lineThickness
            );

            Handles.color = Color.red;
            CustomDrawing.DrawDottedLine(
                Vector3.Lerp(sourceStartPoint, sourceEndPoint, 0.5f),
                Vector3.Lerp(destinationEndPoint, destinationStartPoint, 0.5f),
                lineThickness,
                dashLength
            );
        }

        Handles.BeginGUI();
        GUILayout.BeginVertical("box", GUILayout.Width(200));
        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label("Mismatching edge materials", titleStyle);
        GUILayout.Label("Click to select a material asset");
        var guiPositions = new Vector2[invalidEdges.Count];
        for (var nonMatchingEdgeIndex = 0; nonMatchingEdgeIndex < invalidEdges.Count; ++nonMatchingEdgeIndex)
        {
            var nonMatchingEdge = invalidEdges[nonMatchingEdgeIndex];

            var sourceEdge = nonMatchingEdge.Key;
            var destinationEdge = nonMatchingEdge.Value;

            GUILayout.BeginHorizontal();

            ImguiMaterialButton(sourceEdge.material);
            ImguiMaterialButton(destinationEdge.material);
            var rect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();
             
            var viewActualHeight = view.camera.pixelHeight;
            var guiPosFlippedY = viewActualHeight - rect.center.y * EditorGUIUtility.pixelsPerPoint;
            guiPositions[nonMatchingEdgeIndex] = new Vector2(rect.xMax * EditorGUIUtility.pixelsPerPoint, guiPosFlippedY);
        }
        GUILayout.EndVertical();
        Handles.EndGUI();

        for (var nonMatchingEdgeIndex = 0; nonMatchingEdgeIndex < invalidEdges.Count; ++nonMatchingEdgeIndex)
        {
            var nonMatchingEdge = invalidEdges[nonMatchingEdgeIndex];
            var guiPosition = guiPositions[nonMatchingEdgeIndex];

            var sourceEdge = nonMatchingEdge.Key;
            var destinationEdge = nonMatchingEdge.Value;

            var sourceStartPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)sourceEdge.start);
            var sourceEndPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)sourceEdge.end);

            var destinationStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)destinationEdge.start);
            var destinationEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)destinationEdge.end);

            var guiEntryWorldPosition = view.camera.ScreenToWorldPoint(new Vector3(guiPosition.x, guiPosition.y, view.camera.nearClipPlane));
            var conflictLineCenter = Vector3.Lerp(
                Vector3.Lerp(sourceStartPoint, sourceEndPoint, 0.5f),
                Vector3.Lerp(destinationEndPoint, destinationStartPoint, 0.5f),
            0.5f);
            Handles.DrawLine(guiEntryWorldPosition, conflictLineCenter, lineThickness);
        }
    }

    private void ImguiMaterialButton(Material material)
    {
        var size = 64;
        var width = GUILayout.Width(size);
        var height = GUILayout.Height(size);

        if (GUILayout.Button(AssetPreview.GetAssetPreview(material), width, height))
        {
            Selection.activeObject = material;
        }
    }

    private List<KeyValuePair<Edge, Edge>> matchingEdges;
    private List<KeyValuePair<Edge, Edge>> invalidEdges;
    private List<Edge> invalidOrphanedEdgesOnCurrent;
    private List<Edge> invalidOrphanedEdgesOnConnected;
    private FaceDefinition face;
}

}

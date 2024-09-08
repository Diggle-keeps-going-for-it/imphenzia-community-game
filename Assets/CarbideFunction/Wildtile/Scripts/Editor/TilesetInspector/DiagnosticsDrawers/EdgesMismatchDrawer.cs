using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class EdgesMismatchDrawer : ConnectionsDrawer
{
    public EdgesMismatchDrawer(Vector3 worldDelta, Vector3 tileDimensions, 
        List<KeyValuePair<Edge, Edge>> matchingEdges,
        List<Edge> invalidEdgesOnCurrent,
        List<Edge> invalidEdgesOnDestination,
        FaceDefinition face
    )
        : base(worldDelta, tileDimensions)
    {
        this.matchingEdges = ScaleEdges(matchingEdges);
        this.invalidEdgesOnCurrent = ScaleEdges(invalidEdgesOnCurrent);
        this.invalidEdgesOnDestination = ScaleEdges(invalidEdgesOnDestination);
        this.face = face;
    }

    protected override void DrawConnections(SceneView view)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
        var orientation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, Vector3.up)) * Quaternion.LookRotation(face.gridOffset, Vector3.up);

        var size = 0.02f;
        var lineThickness = UserSettings.instance.tilesetInspectorSettings.lineThickness;
        var offset = UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier - 1f;
        var dashLength = UserSettings.instance.tilesetInspectorSettings.dashedLineDashLength;

        var currentModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(face.gridOffset, tileDimensions) * 0.5f, orientation, Vector3.one);
        var otherModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(face.gridOffset, tileDimensions) * (0.5f + offset), orientation, Vector3.one);

        Handles.color = Color.green;

        foreach (var matchingEdges in matchingEdges)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdges.Key.start);
            var endPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdges.Key.end);
            var otherStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdges.Value.start);
            var otherEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdges.Value.end);

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

            Handles.ConeHandleCap(
                0,
                Vector3.Lerp(startPoint, endPoint, 0.5f),
                Quaternion.LookRotation(startPoint - endPoint),
                size,
                EventType.Repaint
            );
            Handles.ConeHandleCap(
                0,
                Vector3.Lerp(otherStartPoint, otherEndPoint, 0.5f),
                Quaternion.LookRotation(startPoint - endPoint),
                size,
                EventType.Repaint
            );
        }

        Handles.color = Color.blue;
        foreach (var nonMatchingEdge in invalidEdgesOnCurrent)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.start);
            var endPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.end);

            Handles.DrawLine(
                startPoint,
                endPoint,
                lineThickness
            );

            Handles.ConeHandleCap(
                0,
                Vector3.Lerp(startPoint, endPoint, 0.5f),
                Quaternion.LookRotation(startPoint - endPoint),
                size,
                EventType.Repaint
            );
        }

        Handles.color = Color.red;
        foreach (var nonMatchingEdge in invalidEdgesOnDestination)
        {
            var startPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.start);
            var endPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.end);

            Handles.DrawLine(
                startPoint,
                endPoint,
                lineThickness
            );

            Handles.ConeHandleCap(
                0,
                Vector3.Lerp(startPoint, endPoint, 0.5f),
                Quaternion.LookRotation(endPoint - startPoint),
                size,
                EventType.Repaint
            );
        }
    }

    private List<KeyValuePair<Edge, Edge>> matchingEdges;
    private List<Edge> invalidEdgesOnCurrent;
    private List<Edge> invalidEdgesOnDestination;
    private FaceDefinition face;
}

}

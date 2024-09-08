using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class NormalsMismatchDrawer : ConnectionsDrawer
{
    public NormalsMismatchDrawer(Vector3 worldDelta, Vector3 tileDimensions, 
        List<KeyValuePair<Edge, Edge>> matchingEdges,
        List<KeyValuePair<Edge, Edge>> invalidEdges,
        List<Edge> invalidOrphanedEdgesOnCurrent,
        List<Edge> invalidOrphanedEdgesOnConnected,
        FaceDefinition face,
        int normalResolution
    )
        : base(worldDelta, tileDimensions)
    {
        this.matchingEdges = ScaleEdges(matchingEdges);
        this.invalidEdges = ScaleEdges(invalidEdges);
        this.invalidOrphanedEdgesOnCurrent = ScaleEdges(invalidOrphanedEdgesOnCurrent);
        this.invalidOrphanedEdgesOnConnected = ScaleEdges(invalidOrphanedEdgesOnConnected);
        this.face = face;
        this.normalResolution = normalResolution;
    }

    protected override void DrawConnections(SceneView view)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
        var delta = face.gridOffset;
        var orientation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, Vector3.up)) * Quaternion.LookRotation(delta, Vector3.up);

        var lineThickness = UserSettings.instance.tilesetInspectorSettings.lineThickness;
        var offset = UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier - 1f;
        var dashLength = UserSettings.instance.tilesetInspectorSettings.dashedLineDashLength;
        var normalVectorLength = 0.1f;
        var normalWidth = lineThickness;
        var invalidNormalColorLerpToTileColor = 0.8f;

        var currentModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(delta, tileDimensions) * 0.5f, orientation, Vector3.one);
        var otherModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(delta, tileDimensions) * (0.5f + offset), orientation, Vector3.one);

        foreach (var matchingEdge in matchingEdges)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Key.start);
            var endPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Key.end);
            var otherStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Value.start);
            var otherEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)matchingEdge.Value.end);
            var startNormalOffset = currentModuleFaceTransform.MultiplyVector(matchingEdge.Key.startNormal) * normalVectorLength;
            var endNormalOffset = currentModuleFaceTransform.MultiplyVector(matchingEdge.Key.endNormal) * normalVectorLength;

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

            Handles.color = Color.magenta;
            Handles.DrawLine(
                startPoint,
                startPoint + startNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                endPoint,
                endPoint + endNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                otherStartPoint,
                otherStartPoint + startNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                otherEndPoint,
                otherEndPoint + endNormalOffset,
                normalWidth
            );
        }

        foreach (var nonMatchingEdge in invalidEdges)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.Key.start);
            var endPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.Key.end);
            var otherStartPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.Value.start);
            var otherEndPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)nonMatchingEdge.Value.end);

            var startNormalOffset = currentModuleFaceTransform.MultiplyVector(nonMatchingEdge.Key.startNormal) * normalVectorLength;
            var endNormalOffset = currentModuleFaceTransform.MultiplyVector(nonMatchingEdge.Key.endNormal) * normalVectorLength;
            var otherStartNormalOffset = currentModuleFaceTransform.MultiplyVector(nonMatchingEdge.Value.startNormal) * normalVectorLength;
            var otherEndNormalOffset = currentModuleFaceTransform.MultiplyVector(nonMatchingEdge.Value.endNormal) * normalVectorLength;

            Handles.color = Color.red;
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

            Handles.color = Color.red;
            CustomDrawing.DrawDottedLine(
                Vector3.Lerp(startPoint, endPoint, 0.5f),
                Vector3.Lerp(otherEndPoint, otherStartPoint, 0.5f),
                lineThickness,
                dashLength
            );

            Handles.color = Color.Lerp(Color.magenta, Color.red, invalidNormalColorLerpToTileColor);
            Handles.DrawLine(
                startPoint,
                startPoint + startNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                endPoint,
                endPoint + endNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                otherStartPoint,
                otherStartPoint + otherStartNormalOffset,
                normalWidth
            );
            Handles.DrawLine(
                otherEndPoint,
                otherEndPoint + otherEndNormalOffset,
                normalWidth
            );

            Handles.Label(
                startPoint + startNormalOffset,
                nonMatchingEdge.Key.startNormal.Quantize(normalResolution).ToString()
            );
            Handles.Label(
                endPoint + endNormalOffset,
                nonMatchingEdge.Key.endNormal.Quantize(normalResolution).ToString()
            );
            Handles.Label(
                otherStartPoint + otherStartNormalOffset,
                nonMatchingEdge.Value.startNormal.Quantize(normalResolution).ToString()
            );
            Handles.Label(
                otherEndPoint + otherEndNormalOffset,
                nonMatchingEdge.Value.endNormal.Quantize(normalResolution).ToString()
            );
        }
    }

    private List<KeyValuePair<Edge, Edge>> matchingEdges;
    private List<KeyValuePair<Edge, Edge>> invalidEdges;
    private List<Edge> invalidOrphanedEdgesOnCurrent;
    private List<Edge> invalidOrphanedEdgesOnConnected;
    private FaceDefinition face;
    private int normalResolution;
}

}

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class VerticesMismatchDrawer : ConnectionsDrawer
{
    public VerticesMismatchDrawer(Vector3 worldDelta, Vector3 tileDimensions, List<KeyValuePair<Vector2, Vector2>> validVertices, List<Vector2> invalidVerticesOnCurrent, List<Vector2> invalidVerticesOnDestination, FaceDefinition face)
        : base(worldDelta, tileDimensions)
    {
        this.validVertices = ScaleVertices(validVertices);
        this.invalidVerticesOnCurrent = ScaleVertices(invalidVerticesOnCurrent);
        this.invalidVerticesOnDestination = ScaleVertices(invalidVerticesOnDestination);
        this.face = face;
    }

    protected override void DrawConnections(SceneView view)
    {
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Less;
        var orientation = Quaternion.Inverse(Quaternion.LookRotation(Vector3.forward, Vector3.up)) * face.orientation;

        var offset = UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier - 1f;

        var currentModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(orientation * Vector3.forward * 0.5f, tileDimensions), orientation, Vector3.one);
        var otherModuleFaceTransform = Matrix4x4.TRS(Vector3.Scale(orientation * Vector3.forward * (0.5f + offset), tileDimensions), orientation, Vector3.one);

        var size = 0.02f;
        var lineThickness = 0f;

        Handles.color = Color.green;
        foreach (var validVertexPair in validVertices)
        {
            var startPoint = currentModuleFaceTransform.MultiplyPoint((Vector3)validVertexPair.Key);
            var endPoint = otherModuleFaceTransform.MultiplyPoint((Vector3)validVertexPair.Value);
            Handles.SphereHandleCap(
                0,
                startPoint,
                Quaternion.identity,
                size,
                EventType.Repaint
            );
            
            Handles.DrawLine(
                startPoint,
                endPoint,
                lineThickness
            );

            Handles.SphereHandleCap(
                0,
                endPoint,
                Quaternion.identity,
                size,
                EventType.Repaint
            );
        }

        Handles.color = Color.blue;
        foreach (var invalidVertex in invalidVerticesOnCurrent)
        {
            Handles.SphereHandleCap(
                0,
                currentModuleFaceTransform.MultiplyPoint((Vector3)invalidVertex),
                Quaternion.identity,
                size,
                EventType.Repaint
            );
        }

        Handles.color = Color.red;
        foreach (var invalidVertex in invalidVerticesOnDestination)
        {
            Handles.SphereHandleCap(
                0,
                otherModuleFaceTransform.MultiplyPoint((Vector3)invalidVertex),
                Quaternion.identity,
                size,
                EventType.Repaint
            );
        }
    }

    private FaceDefinition face;
    private List<KeyValuePair<Vector2, Vector2>> validVertices = new List<KeyValuePair<Vector2, Vector2>>();
    private List<Vector2> invalidVerticesOnCurrent = new List<Vector2>();
    private List<Vector2> invalidVerticesOnDestination = new List<Vector2>();
}

}

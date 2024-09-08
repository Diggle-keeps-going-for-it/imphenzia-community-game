using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class UnknownMismatchDrawer : ConnectionsDrawer
{
    public UnknownMismatchDrawer(Vector3 worldDelta, Vector3 tileDimensions)
        : base(worldDelta, tileDimensions, isError:true)
    {
    }

    protected override void DrawConnections(SceneView view)
    {
        Handles.Label(Vector3.right, "Unknown mismatch\n\nReimporting may fix this if the models have changed since the tileset was last updated.");
    }
}

}

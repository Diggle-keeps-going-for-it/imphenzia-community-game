using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class ValidConnectionsDrawer : ConnectionsDrawer
{
    public ValidConnectionsDrawer(Vector3 worldDelta, Vector3 tileDimensions)
        : base(worldDelta, tileDimensions, isError:false)
    {
    }

    protected override void DrawConnections(SceneView view)
    {
        // intentionally empty
    }
}

}

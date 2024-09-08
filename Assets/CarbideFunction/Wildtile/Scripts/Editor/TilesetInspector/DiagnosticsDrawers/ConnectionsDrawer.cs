using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal abstract class ConnectionsDrawer : IDiagnosticsDrawer
{
    public ConnectionsDrawer(Vector3 worldDelta, Vector3 tileDimensions, bool isError = true)
    {
        this.worldDelta = worldDelta;
        this.tileDimensions = tileDimensions;
        this.isError = isError;
    }

    void IDiagnosticsDrawer.Draw(SceneView view)
    {
        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, tileDimensions);

        DrawConnections(view);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;

        Handles.color = Color.red;
        Handles.DrawWireCube(worldDelta * OffsetMultiplier, tileDimensions);
    }

    void IDiagnosticsDrawer.Shutdown()
    {}

    private float OffsetMultiplier => isError ? UserSettings.instance.tilesetInspectorSettings.errorPreviewOffsetMultiplier : 1f;

    protected abstract void DrawConnections(SceneView view);

    protected Vector3 worldDelta
    {
        get;
        private set;
    }
    protected Vector3 tileDimensions
    {
        get;
        private set;
    }
    protected bool isError
    {
        get;
        private set;
    }

    protected List<KeyValuePair<Vector2, Vector2>> ScaleVertices(List<KeyValuePair<Vector2, Vector2>> sourceVertices)
    {
        return sourceVertices.Select(vertPair => new KeyValuePair<Vector2, Vector2>(ScaleByDimensions(vertPair.Key), ScaleByDimensions(vertPair.Value))).ToList();
    }

    protected List<Vector2> ScaleVertices(List<Vector2> sourceVertices)
    {
        return sourceVertices.Select(vert => ScaleByDimensions(vert)).ToList();
    }

    private Vector2 ScaleByDimensions(Vector2 value)
    {
        return Vector2.Scale(value, new Vector2(tileDimensions.x, tileDimensions.y));
    }

    protected List<KeyValuePair<Edge, Edge>> ScaleEdges(List<KeyValuePair<Edge, Edge>> sourceEdges)
    {
        return sourceEdges.Select(edgePair => new KeyValuePair<Edge, Edge>(ScaleByDimensions(edgePair.Key), ScaleByDimensions(edgePair.Value))).ToList();
    }

    protected List<Edge> ScaleEdges(List<Edge> sourceEdges)
    {
        return sourceEdges.Select(edge => ScaleByDimensions(edge)).ToList();
    }

    private Edge ScaleByDimensions(Edge edge)
    {
        return new Edge{
            start = ScaleByDimensions(edge.start),
            startNormal = ScaleNormalByDimensions(edge.startNormal),
            end = ScaleByDimensions(edge.end),
            endNormal = ScaleNormalByDimensions(edge.endNormal),
            material = edge.material,
        };
    }

    private Vector3 ScaleNormalByDimensions(Vector3 normal)
    {
        var xScalar = new Vector3(1f, tileDimensions.y, tileDimensions.z);
        var yScalar = new Vector3(tileDimensions.x, 1f, tileDimensions.z);
        var zScalar = new Vector3(tileDimensions.x, tileDimensions.y, 1f);

        return Vector3.Scale(xScalar, Vector3.Scale(yScalar, Vector3.Scale(zScalar, normal))).normalized;
    }
}

}

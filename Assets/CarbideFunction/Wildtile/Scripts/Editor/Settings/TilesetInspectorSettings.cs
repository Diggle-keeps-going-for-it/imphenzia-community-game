using System;
using UnityEngine;
using UnityEditor;

using CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer;

namespace CarbideFunction.Wildtile.Editor
{

[SerializableAttribute]
internal class TilesetInspectorSettings
{
    public delegate void OnPropertyChanged();

    [SerializeField]
    [Min(0f)]
    public float lineThickness = 3f;

    public const float dashedLineDashLengthMin = 1e-5f;
    [SerializeField]
    [Min(dashedLineDashLengthMin)]
    public float dashedLineDashLength = 0.02f;

    [SerializeField]
    [Min(1f)]
    public float errorPreviewOffsetMultiplier = 1.5f;

    private const int currentModulesListViewHeightMinimum = 1;
    [SerializeField]
    [Min(currentModulesListViewHeightMinimum)]
    public int currentModulesListViewHeight = 200;
    public OnPropertyChanged onCurrentModulesListViewHeightChanged = null;

    [SerializeField]
    internal VertexContentsDrawer.Data vertexContentsDrawer = new VertexContentsDrawer.Data();

    [SerializeField]
    internal VertexContentsSummaryDrawer.Data vertexContentsSummaryDrawer = new VertexContentsSummaryDrawer.Data();

    [SerializeField]
    internal OutOfBoundsDrawer.Data outOfBoundsVerticesDrawer = new OutOfBoundsDrawer.Data();

    public void OnGui()
    {
        lineThickness = Mathf.Max(0f, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(lineThickness)), lineThickness));
        dashedLineDashLength = Mathf.Max(dashedLineDashLengthMin, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(dashedLineDashLength)), dashedLineDashLength));
        errorPreviewOffsetMultiplier = Mathf.Max(1f, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(errorPreviewOffsetMultiplier)), errorPreviewOffsetMultiplier));

        EditorGUI.BeginChangeCheck();
        currentModulesListViewHeight = Math.Max(currentModulesListViewHeightMinimum, EditorGUILayout.IntField(ObjectNames.NicifyVariableName(nameof(currentModulesListViewHeight)), currentModulesListViewHeight));
        if (EditorGUI.EndChangeCheck())
        {
            onCurrentModulesListViewHeightChanged?.Invoke();
        }

        vertexContentsDrawer.OnGui();
        vertexContentsSummaryDrawer.OnGui();
        outOfBoundsVerticesDrawer.OnGui();
    }
}

}

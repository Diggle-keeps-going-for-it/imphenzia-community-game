using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class VertexContentsSummaryDrawer : IDiagnosticsDrawer
{
    [Serializable]
    public class Data
    {
        public float indicatorCubeSize = 0.05f;

        [SerializeField]
        private bool isExpandedInOptionsMenu = true;

        public void OnGui()
        {
            isExpandedInOptionsMenu = EditorGUILayout.BeginFoldoutHeaderGroup(isExpandedInOptionsMenu, "Vertex Contents Drawer");
            if (isExpandedInOptionsMenu)
            {
                EditorGUI.BeginChangeCheck();
                indicatorCubeSize = Mathf.Max(0f, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(indicatorCubeSize)), indicatorCubeSize));
                if (EditorGUI.EndChangeCheck())
                {
                    RedrawSceneViews.Redraw();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

    public VertexContentsSummaryDrawer(int[] vertexContents, Vector3 dimensions, Data data)
    {
        this.vertexContents = vertexContents;
        this.dimensions = dimensions;
        this.data = data;
    }

    void IDiagnosticsDrawer.Draw(SceneView view)
    {
        DrawVertexContents();
    }

    void IDiagnosticsDrawer.Shutdown()
    {}

    private void DrawVertexContents()
    {
        for (var x = 0; x < 2; ++x)
        {
            for (var y = 0; y < 2; ++y)
            {
                for (var z = 0; z < 2; ++z)
                {
                    var contentsIndex = x + 2*y + 4*z;
                    var cornerContents = vertexContents[contentsIndex];

                    var positiveOneZeroOffset = new Vector3(x, y, z);
                    var cornerOffset = Vector3.Scale(positiveOneZeroOffset - Vector3.one * 0.5f, dimensions);

                    DrawContentsCube(cornerContents, cornerOffset);
                }
            }
        }
    }

    private void DrawContentsCube
    (
        int contents,
        Vector3 cubeCenter
    )
    {
        if (contents <= 0)
        {
            DrawEmptyCube(cubeCenter);
        }
        else
        {
            DrawFilledCube(cubeCenter);
        }
    }

    private void DrawEmptyCube(Vector3 position)
    {
        Handles.color = emptyColor;
        Handles.DrawWireCube(position, Vector3.one * data.indicatorCubeSize);
    }

    private void DrawFilledCube(Vector3 position)
    {
        Handles.color = Color.blue;

        // top
        DrawCubeFace(position, Vector3.up, Vector3.right);
        // sides
        DrawCubeFace(position, Vector3.right, Vector3.back);
        DrawCubeFace(position, Vector3.left,  Vector3.back);
        DrawCubeFace(position, Vector3.forward, Vector3.right);
        DrawCubeFace(position, Vector3.back,    Vector3.right);
        // bottom
        DrawCubeFace(position, Vector3.down, Vector3.right);
    }

    private void DrawCubeFace(Vector3 position, Vector3 zVector, Vector3 yVector)
    {
        var xVector = Vector3.Cross(zVector, yVector);
        var transform = Matrix4x4.zero;
        transform.SetColumn(0, xVector);
        transform.SetColumn(1, yVector);
        transform.SetColumn(2, zVector);
        transform.SetColumn(3, new Vector4(0f, 0f, 0f, 1f));

        var unitCubeFaceVerts = new []{
            new Vector3(-.5f,  .5f, .5f),
            new Vector3( .5f,  .5f, .5f),
            new Vector3( .5f, -.5f, .5f),
            new Vector3(-.5f, -.5f, .5f),
        };

        var verts = unitCubeFaceVerts.Select(vert => position + transform.MultiplyPoint3x4(vert) * data.indicatorCubeSize).ToArray();

        Handles.DrawSolidRectangleWithOutline(verts, filledColor, Color.clear);
    }

    private static readonly Color filledColor = Color.blue;
    private static readonly Color emptyColor = Color.yellow;

    private int[] vertexContents;
    private Vector3 dimensions;
    private Data data;
}

}

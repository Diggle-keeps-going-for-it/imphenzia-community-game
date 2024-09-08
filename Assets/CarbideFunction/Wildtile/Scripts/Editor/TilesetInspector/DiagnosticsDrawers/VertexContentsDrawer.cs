using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal class VertexContentsDrawer : IDiagnosticsDrawer
{
    [Serializable]
    public class Data
    {
        public float superInsideSplitDistance = 0.01f;
        public const string showIntersectionPointsTooltip = "Highlight the points where the insideness ray hits faces. They will be indicated as small circles, yellow circles show that the ray is exiting a volume and blue circles show it is entering a volume.";
        [Tooltip(showIntersectionPointsTooltip)]
        public bool showIntersectionPoints = true;
        public float intersectionPointRadius = 0.05f;

        public const string intersectionNarrowingTooltip = "Each face intersection indicator circle can be slightly smaller than the one before it. At 0 narrowing, all indicators will be the same size; at 1 indicators will quickly shrink and the indicator closest to the corner will be narrowed to zero width.";
        [Tooltip(intersectionNarrowingTooltip)]
        public float intersectionNarrowing = 0.2f;
        public bool showArrowhead = true;

        [SerializeField]
        private bool isExpandedInOptionsMenu = true;

        public void OnGui()
        {
            isExpandedInOptionsMenu = EditorGUILayout.BeginFoldoutHeaderGroup(isExpandedInOptionsMenu, "Vertex Contents Drawer");
            if (isExpandedInOptionsMenu)
            {
                EditorGUI.BeginChangeCheck();
                superInsideSplitDistance = Mathf.Max(0f, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(superInsideSplitDistance)), superInsideSplitDistance));
                showIntersectionPoints = EditorGUILayout.Toggle(
                    new GUIContent(
                        text: ObjectNames.NicifyVariableName(nameof(showIntersectionPoints)),
                        tooltip: showIntersectionPointsTooltip
                    ), showIntersectionPoints);
                intersectionPointRadius = Mathf.Max(0f, EditorGUILayout.FloatField(ObjectNames.NicifyVariableName(nameof(intersectionPointRadius)), intersectionPointRadius));
                intersectionNarrowing = EditorGUILayout.Slider(
                    new GUIContent(
                        text: ObjectNames.NicifyVariableName(nameof(intersectionNarrowing)),
                        tooltip: intersectionNarrowingTooltip
                    ),
                    intersectionNarrowing, 0f, 1f);
                showArrowhead = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(nameof(showArrowhead)), showArrowhead);
                if (EditorGUI.EndChangeCheck())
                {
                    RedrawSceneViews.Redraw();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }
    }

    public class CrossedTriangle
    {
        public Triangle triangle;
        public Vector3 hitWorldPosition;
        public bool isHittingFrontFace;
    }

    public VertexContentsDrawer(List<CrossedTriangle> crossedTriangles, Vector3 lineEnd, Data data)
    {
        this.crossedTriangles = crossedTriangles;
        this.lineEnd = lineEnd;
        this.data = data;
    }

    void IDiagnosticsDrawer.Draw(SceneView view)
    {
        if (crossedTriangles.Count > 0)
        {
            DrawCrossedTriangles(view.camera.transform.forward);
        }
        else
        {
            DrawNoCrossedTriangles();
        }
    }

    private void DrawCrossedTriangles(Vector3 cameraViewDirection)
    {
        Assert.IsTrue(crossedTriangles.Count > 0);

        var firstCrossedTri = crossedTriangles.First();
        DrawCrossedTriangle(firstCrossedTri, 0, crossedTriangles.Count);
        var lastHitPoint = firstCrossedTri.hitWorldPosition;
        var runningInsideness = firstCrossedTri.isHittingFrontFace ? 1 : 0;
        for (var crossedTriIndex = 1; crossedTriIndex < crossedTriangles.Count; ++crossedTriIndex)
        {
            var crossedTri = crossedTriangles[crossedTriIndex];
            DrawCrossedTriangle(crossedTri, crossedTriIndex, crossedTriangles.Count);
            DrawRunningTotalInsidenessLine(runningInsideness, lastHitPoint, crossedTri.hitWorldPosition, cameraViewDirection);
            lastHitPoint = crossedTri.hitWorldPosition;
            runningInsideness += crossedTri.isHittingFrontFace ? 1 : -1;
        }
        
        DrawRunningTotalInsidenessLine(runningInsideness, lastHitPoint, lineEnd, cameraViewDirection);
        DrawArrowheadIfEnabled(runningInsideness, lineEnd, lineEnd - firstCrossedTri.hitWorldPosition);
    }

    private void DrawNoCrossedTriangles()
    {
        DrawArrowheadIfEnabled(0, lineEnd, lineEnd);
    }

    void IDiagnosticsDrawer.Shutdown()
    {}

    private void DrawCrossedTriangle(CrossedTriangle tri, int intersectionIndex, int totalIntersections)
    {
        Handles.color = tri.isHittingFrontFace ? crossedPositiveColor : crossedNegativeColor;
        DrawTriangle(tri.triangle);
        DrawIntersectionPointIfEnabledBySettings(tri, intersectionIndex, totalIntersections);
    }

    private void DrawTriangle(Triangle tri)
    {
        Handles.DrawLine(tri.vertex0, tri.vertex1, 0);
        Handles.DrawLine(tri.vertex1, tri.vertex2, 0);
        Handles.DrawLine(tri.vertex2, tri.vertex0, 0);
    }

    private void DrawRunningTotalInsidenessLine(int currentInsideness, Vector3 lastPosition, Vector3 nextPosition, Vector3 cameraViewDirection)
    {
        var isInside = currentInsideness > 0;
        Handles.color = isInside ? crossedPositiveColor : crossedNegativeColor;
        var numberOfLines = isInside ? currentInsideness : 1 + (-currentInsideness);
        Assert.IsTrue(numberOfLines >= 1);

        var lineDirection = nextPosition - lastPosition;
        var offsetDirection = Vector3.Cross(cameraViewDirection, lineDirection).normalized;
        var startingOffset = (-data.superInsideSplitDistance * (numberOfLines - 1) * 0.5f) * offsetDirection;
        
        for (var lineIndex = 0; lineIndex < numberOfLines; ++lineIndex)
        {
            var currentOffset = startingOffset + data.superInsideSplitDistance * offsetDirection * lineIndex;
            Handles.DrawLine(lastPosition + currentOffset, nextPosition + currentOffset, 0);
        }
    }

    private void DrawArrowheadIfEnabled(int currentInsideness, Vector3 position, Vector3 direction)
    {
        if (data.showArrowhead)
        {
            DrawArrowhead(currentInsideness, position, direction);
        }
    }

    private void DrawArrowhead(int currentInsideness, Vector3 position, Vector3 direction)
    {
        var isInside = currentInsideness > 0;
        Handles.color = isInside ? crossedPositiveColor : crossedNegativeColor;
        var arrowheadSize = 0.05f;
        var arrowheadCenterOffsetFromTip = arrowheadSize * 0.66f;
        var arrowheadCenter = position - direction.normalized * arrowheadCenterOffsetFromTip;
        Handles.ConeHandleCap(0, arrowheadCenter, Quaternion.LookRotation(direction), arrowheadSize, EventType.Repaint);
    }

    private void DrawIntersectionPointIfEnabledBySettings(CrossedTriangle triangle, int intersectionIndex, int totalIntersections)
    {
        if (data.showIntersectionPoints)
        {
            DrawIntersectionPoint(triangle, intersectionIndex, totalIntersections);
        }
    }

    private void DrawIntersectionPoint(CrossedTriangle triangle, int intersectionIndex, int totalIntersections)
    {
        var triangleNormal = Vector3.Cross(triangle.triangle.vertex1 - triangle.triangle.vertex0, triangle.triangle.vertex2 - triangle.triangle.vertex0);

        var radius = data.intersectionPointRadius * CalculateIntersectionIndicatorRadiusMultiplier(intersectionIndex, totalIntersections);
        Handles.DrawWireDisc(triangle.hitWorldPosition, triangleNormal, radius, 0f);
    }

    private float CalculateIntersectionIndicatorRadiusMultiplier(int intersectionIndex, int totalIntersections)
    {
        if (totalIntersections == 1)
        {
            return 1f;
        }

        // note that this is proportion through indices and not proportion through the line's distance
        // this allows users to spot overlapping triangles.
        var proportionThroughIndices = ((float)intersectionIndex) / (totalIntersections - 1);
        return Mathf.Lerp(1f, 1f - data.intersectionNarrowing, proportionThroughIndices);
    }

    private static readonly Color crossedPositiveColor = Color.blue;
    private static readonly Color crossedNegativeColor = Color.yellow;
    private static readonly Color missedColor = Color.white;

    private List<CrossedTriangle> crossedTriangles;
    private Vector3 lineEnd;
    private Data data;
}

}

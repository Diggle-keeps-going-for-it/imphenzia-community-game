using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
internal static class CustomDrawing
{
    public static void DrawDottedLine(Vector3 start, Vector3 end, float lineThickness, float dashLength)
    {
        if (end == start)
        {
            return;
        }

        if (dashLength <= 0f)
        {
            Handles.DrawLine(
                start,
                end,
                lineThickness
            );

            return;
        }

        var lineOffset = end - start;
        var lineLength = lineOffset.magnitude;
        var lineDirection = lineOffset / lineLength;

        for (var distance = 0f; distance < lineLength; distance += dashLength * 2f)
        {
            var dashStartPoint = start + distance * lineDirection;
            var endDistance = Mathf.Min(distance + dashLength, lineLength);
            var dashEndPoint = start + endDistance * lineDirection;

            Handles.DrawLine(
                dashStartPoint,
                dashEndPoint,
                lineThickness
            );
        }
    }
}
}

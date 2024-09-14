using UnityEditor;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaRandomRotation))]
    [CanEditMultipleObjects]
    public class VariaRandomRotationEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var t = target as VariaRandomRotation;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("point"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("axis"));
            EditorUtility.NiceRange(serializedObject, "min", "max", -180, 180, "Range");
            EditorUtility.NiceRange(serializedObject, "dispersionMin", "dispersionMax", 0, 180, "Dispersion Range");

            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            Handles.color = Color.red;
            var t = target as VariaRandomRotation;

            var worldPoint = t.transform.TransformPoint(t.point);
            var worldAxis = t.transform.TransformDirection(t.axis);

            var axis2 = Vector3.Cross(worldAxis, Vector3.forward);
            if (axis2.sqrMagnitude == 0) axis2 = Vector3.Cross(t.axis, Vector3.right);
            axis2.Normalize();

            var minVec = Quaternion.AngleAxis(t.min, worldAxis) * axis2;
            var maxVec = Quaternion.AngleAxis(t.max, worldAxis) * axis2;

            var handleSize = HandleUtility.GetHandleSize(worldPoint);

            if (t.min != 0 || t.max != 0)
            {
                Handles.DrawLine(worldPoint, worldPoint + axis2 * handleSize);
                Handles.DrawLine(worldPoint, worldPoint + minVec * handleSize);
                Handles.DrawLine(worldPoint, worldPoint + maxVec * handleSize);
                Handles.DrawWireArc(worldPoint, worldAxis, minVec, t.max - t.min, handleSize);
            }

            if (t.dispersionMin != 0)
            {
                Handles.DrawWireDisc(worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMin), worldAxis, handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMin));
            }

            if (t.dispersionMax != 0)
            {
                Handles.DrawWireDisc(worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMax), worldAxis, handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMax));
            }

            // Doesn't look good
            /*
            if(t.dispersionMin != t.dispersionMax)
            {
                var k = 16;
                for (var i = 0; i < k; i++)
                {
                    var v1 = Quaternion.AngleAxis(i * 360f / k, t.axis) * axis2;
                    var v2 = Quaternion.AngleAxis((i + 1) * 360f / k, t.axis) * axis2;
                    var p1 = worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMin) + v1 * handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMin);
                    var p2 = worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMin) + v2 * handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMin);
                    var p3 = worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMax) + v2 * handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMax);
                    var p4 = worldPoint + worldAxis * handleSize * Mathf.Cos(Mathf.Deg2Rad * t.dispersionMax) + v1 * handleSize * Mathf.Sin(Mathf.Deg2Rad * t.dispersionMax);
                    Handles.DrawAAConvexPolygon(p1, p2, p3, p4);
                }
            }
            */
        }
    }
}
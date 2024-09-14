using UnityEditor;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaRandomScale))]
    [CanEditMultipleObjects]
    public class VariaRandomScaleEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            var t = target as VariaRandomScale;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("linked"));
            if (t.linked)
            {
                EditorUtility.NiceRange(serializedObject, "minX", "maxX", 0, 2, "Scale", "Min", "Max");
            }
            else
            {
                EditorUtility.NiceRange(serializedObject, "minX", "maxX", 0, 2, "X");
                EditorUtility.NiceRange(serializedObject, "minY", "maxY", 0, 2, "Y");
                EditorUtility.NiceRange(serializedObject, "minZ", "maxZ", 0, 2, "Z");
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("scaleOrigin"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));

            serializedObject.ApplyModifiedProperties();
        }

        /*
        void OnSceneGUI()
        {
            var t = target as VariaRandomScale;

            var worldPoint = t.transform.TransformPoint(t.scaleOrigin);

            EditorGUI.BeginChangeCheck();
            var p = Handles.PositionHandle(worldPoint, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                t.scaleOrigin = t.transform.InverseTransformPoint(p);
            }
        }
        */
    }
}
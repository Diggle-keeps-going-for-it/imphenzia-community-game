using UnityEditor;
using UnityEngine;

namespace Varia
{
    public static class EditorUtility
    {
        public static void NiceRange(SerializedObject serializedObject, string minName, string maxName, float minLimit, float maxLimit, string displayName)
        {
            var minProperty = serializedObject.FindProperty(minName);
            var maxProperty = serializedObject.FindProperty(maxName);
            var min = minProperty.floatValue;
            var max = maxProperty.floatValue;
            EditorGUILayout.MinMaxSlider(displayName, ref min, ref max, minLimit, maxLimit);
            if (min != minProperty.floatValue)
            {
                minProperty.floatValue = min;
            }
            if (max != maxProperty.floatValue)
            {
                maxProperty.floatValue = max;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minProperty);
            EditorGUILayout.PropertyField(maxProperty);
            EditorGUI.indentLevel--;
        }

        public static void NiceRange(SerializedObject serializedObject, string minName, string maxName, float minLimit, float maxLimit, string displayName, string minDisplayName, string maxDisplayName)
        {
            var minProperty = serializedObject.FindProperty(minName);
            var maxProperty = serializedObject.FindProperty(maxName);
            var min = minProperty.floatValue;
            var max = maxProperty.floatValue;
            EditorGUILayout.MinMaxSlider(displayName, ref min, ref max, minLimit, maxLimit);
            if (min != minProperty.floatValue)
            {
                minProperty.floatValue = min;
            }
            if (max != maxProperty.floatValue)
            {
                maxProperty.floatValue = max;
            }
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(minProperty, new GUIContent(minDisplayName));
            EditorGUILayout.PropertyField(maxProperty, new GUIContent(maxDisplayName));
            EditorGUI.indentLevel--;
        }
    }
}
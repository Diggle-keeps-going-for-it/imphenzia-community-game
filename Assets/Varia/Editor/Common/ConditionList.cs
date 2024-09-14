using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{

    internal class ConditionList
    {
        public ReorderableList rl;

        public ConditionList(SerializedObject serializedObject, SerializedProperty serializedProperty)
        {
            rl = new ReorderableList(serializedObject, serializedProperty, true, false, true, true);

            rl.headerHeight = 0;

            rl.drawElementCallback += (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                VariaConditionDrawer.DoGUI(rect, serializedProperty.GetArrayElementAtIndex(index));
            };

            rl.elementHeightCallback += (int index) =>
            {
                return VariaConditionDrawer.GetHeight(serializedProperty.GetArrayElementAtIndex(index));
            };

            rl.drawNoneElementCallback += (Rect rect) =>
            {
                EditorGUI.LabelField(rect, new GUIContent("No conditions - always runs"));
            };
        }

        public ConditionList(SerializedObject serializedObject) : this(serializedObject, serializedObject.FindProperty("conditionsList.conditions")) { }

        public void DrawLayout()
        {
            var serializedProperty = rl.serializedProperty;
            var displayName = serializedProperty.displayName + (rl.count == 0 ? " (empty)" : $" ({rl.count})");
            serializedProperty.isExpanded = EditorGUILayout.Foldout(serializedProperty.isExpanded, displayName);

            if (serializedProperty.isExpanded)
            {
                rl.DoLayoutList();
            }
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && rl.index >= 0)
            {
                serializedProperty.DeleteArrayElementAtIndex(rl.index);
                if (rl.index >= serializedProperty.arraySize - 1)
                {
                    rl.index = serializedProperty.arraySize - 1;
                }
            }
        }

    }
}
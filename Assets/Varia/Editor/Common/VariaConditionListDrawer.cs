using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{
    [CustomPropertyDrawer(typeof(VariaConditionList))]
    public class VariaConditionListDrawer: PropertyDrawer
    {
        private Dictionary<string, ConditionList> states = new Dictionary<string, ConditionList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var foldoutRect = position;
            foldoutRect.height = EditorGUIUtility.singleLineHeight;
            var arrayProperty = property.FindPropertyRelative("conditions");
            label.text = label.text + (arrayProperty.arraySize == 0 ? " (empty)" : $" ({arrayProperty.arraySize})");
            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            position.yMin += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            if (property.isExpanded)
            {
                GetList(property).rl.DoList(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + (property.isExpanded ? GetList(property).rl.GetHeight() : 0);
        }

        private ConditionList GetList(SerializedProperty prop)
        {
            var propertyPath = prop.propertyPath;

            if(!states.TryGetValue(propertyPath, out var state))
            {
                state = states[propertyPath] = new ConditionList(prop.serializedObject, prop.FindPropertyRelative("conditions"));
            }

            return state;
        }
    }
}
 
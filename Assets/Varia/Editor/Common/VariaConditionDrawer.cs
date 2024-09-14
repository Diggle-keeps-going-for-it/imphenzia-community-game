using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{

    [CustomPropertyDrawer(typeof(VariaCondition))]
    public class VariaConditionDrawer : PropertyDrawer
    {
        private static float PaddedLineHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var y = position.y;
            EditorGUI.LabelField(At(position, ref y), label);
            position.yMin = y;
            EditorGUI.indentLevel++;
            DoGUI(position, property);
            EditorGUI.indentLevel--;
        }

        private static Rect At(Rect position, ref float y, float height)
        {
            var r = new Rect(position.x, y, position.width, height);
            y += height;
            return r;
        }

        private static Rect At(Rect position, ref float y)
        {
            return At(position, ref y, PaddedLineHeight);
        }


        public static void DoGUI(Rect position, SerializedProperty property)
        {
            var y = position.y;
            y += EditorGUIUtility.standardVerticalSpacing;
            EditorGUI.PropertyField(At(position, ref y), property.FindPropertyRelative("conditionType"));
            var conditionType = (VariaConditionType)property.FindPropertyRelative("conditionType").enumValueIndex;
            switch (conditionType)
            {
                case VariaConditionType.Random:
                    EditorGUI.PropertyField(At(position, ref y), property.FindPropertyRelative("randomChance"));
                    break;
                case VariaConditionType.DepthFilter:
                    EditorGUI.PropertyField(At(position, ref y), property.FindPropertyRelative("comparison"));
                    EditorGUI.PropertyField(At(position, ref y), property.FindPropertyRelative("depth"));
                    break;
            }
        }

        public static float GetHeight(SerializedProperty property)
        {
            var conditionType = (VariaConditionType)property.FindPropertyRelative("conditionType").enumValueIndex;
            switch (conditionType)
            {
                case VariaConditionType.Random:
                    return EditorGUIUtility.standardVerticalSpacing + PaddedLineHeight * 2;
                case VariaConditionType.DepthFilter:
                    return EditorGUIUtility.standardVerticalSpacing + PaddedLineHeight * 3;
            }
            return 0;
        }

        //http://answers.unity.com/comments/1262953/view.html
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => GetHeight(property) + PaddedLineHeight;
    }
}
 
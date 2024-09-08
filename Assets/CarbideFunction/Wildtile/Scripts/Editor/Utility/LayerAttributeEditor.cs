using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    internal class LayerAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.intValue = EditorGUI.LayerField(position, label,  property.intValue);
        }
    }
}

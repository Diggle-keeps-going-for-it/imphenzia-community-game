using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{

    [CustomEditor(typeof(VariaCopyValue))]
    public class VariaCopyValueEditor : Editor
    {
        private TargetPropertyEditor srcTargetPropertyEditor;
        private TargetPropertyEditor destTargetPropertyEditor;

        private void OnEnable()
        {
            srcTargetPropertyEditor = new TargetPropertyEditor(serializedObject, serializedObject.FindProperty("srcTarget"), serializedObject.FindProperty("srcProperty"));
            destTargetPropertyEditor = new TargetPropertyEditor(serializedObject, serializedObject.FindProperty("destTarget"), serializedObject.FindProperty("destProperty"));
        }

        public override void OnInspectorGUI()
        {
            srcTargetPropertyEditor.GUI();
            destTargetPropertyEditor.GUI();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaPreviewer))]
    [CanEditMultipleObjects]
    public class VariaPreviewerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            var previewer = target as VariaPreviewer;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("continuousRefresh"));
            if (previewer.continuousRefresh)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshBufferTime"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("refreshInEditor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("seed"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));

            serializedObject.ApplyModifiedProperties();

            if(GUILayout.Button("Referesh"))
            {
                previewer.Refresh();
            }
        }
    }
}
using UnityEditor;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaRandomPosition))]
    [CanEditMultipleObjects]
    public class VariaRandomPositionEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            var t = target as VariaRandomPosition;
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeTo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxX"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minY"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxY"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minZ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxZ"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));

            serializedObject.ApplyModifiedProperties();
        }

        void OnSceneGUI()
        {
            var t = target as VariaRandomPosition;
            Matrix4x4 mat;
            switch (t.relativeTo)
            {
                case RelativeTo.Local:
                    mat = t.transform.localToWorldMatrix;
                    break;
                case RelativeTo.Parent:
                    mat = t.transform.parent?.localToWorldMatrix ?? t.transform.localToWorldMatrix;
                    break;
                case RelativeTo.World:
                    mat = Matrix4x4.identity;
                    break;
                default:
                    throw new System.Exception();
            }

            var worldPoint = t.transform.position;

            // Only take rotation.
            mat.SetColumn(3, worldPoint);
            mat.m33 = 1.0f;

            var handleSize = HandleUtility.GetHandleSize(worldPoint);

            var min = new Vector3(t.minX, t.minY, t.minZ);
            var max = new Vector3(t.maxX, t.maxY, t.maxZ);

            Handles.matrix = mat;
            Handles.DrawWireCube((min + max) * 0.5f, (max - min));
            Handles.matrix = Matrix4x4.identity;
        }
    }
}
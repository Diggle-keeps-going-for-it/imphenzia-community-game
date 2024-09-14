using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaInstantiate))]
    [CanEditMultipleObjects]
    public class VariaInstantiateEditor : Editor
    {
        SerializedProperty list;

        ReorderableList rl;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;

        bool useWeights;

        private void AddNull()
        {
            ++list.arraySize;
            list.GetArrayElementAtIndex(list.arraySize - 1).FindPropertyRelative("weight").floatValue = 1.0f;
        }

        public void OnEnable()
        {
            list = serializedObject.FindProperty("targets");
            list.isExpanded = true;
            if(list.arraySize == 0)
            {
                AddNull();
                serializedObject.ApplyModifiedProperties();
            }

            rl = new ReorderableList(serializedObject, list, true, false, true, true);

            rl.headerHeight = 0;

            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var gameObjectProperty = targetElement.FindPropertyRelative("gameObject");
                var weightProperty = targetElement.FindPropertyRelative("weight");

                var tileRect = rect;
                tileRect.height = EditorGUI.GetPropertyHeight(gameObjectProperty);
                var weightRect = rect;
                weightRect.yMin = tileRect.yMax + k_fieldPadding;
                weightRect.height = EditorGUI.GetPropertyHeight(weightProperty);
                EditorGUI.PropertyField(tileRect, gameObjectProperty);
                if (useWeights)
                {
                    EditorGUI.PropertyField(weightRect, weightProperty);
                }
            };

            rl.elementHeightCallback = (int index) =>
            {
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var tileProperty = targetElement.FindPropertyRelative("gameObject");
                if (useWeights)
                {
                    var weightProperty = targetElement.FindPropertyRelative("weight");
                    return EditorGUI.GetPropertyHeight(tileProperty) + k_fieldPadding + EditorGUI.GetPropertyHeight(weightProperty) + k_elementPadding;
                }
                else
                {
                    return EditorGUI.GetPropertyHeight(tileProperty) + k_elementPadding;
                }
            };

            rl.onAddCallback = l =>
            {
                AddNull();
                rl.index = rl.serializedProperty.arraySize - 1;
            };

        }

        public override void OnInspectorGUI()
        {
            var t = target as VariaInstantiate;
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("useWeights"));
            useWeights = serializedObject.FindProperty("useWeights").boolValue;
            List();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("thenDestroyThis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));

            if (!(target as VariaBehaviour).conditionList.conditions.Any(x=>x.conditionType == VariaConditionType.DepthFilter))
            {
                EditorGUILayout.HelpBox("Using Instantiate without a depth filter can cause infinite recursion.", MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void List()
        {
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent("Targets"));

            if (list.isExpanded)
            {
                var r1 = GUILayoutUtility.GetLastRect();

                rl.DoLayoutList();

                var r2 = GUILayoutUtility.GetLastRect();

                var r = new Rect(r1.xMin, r1.yMax, r1.width, r2.yMax - r1.yMax);

                if (r.Contains(Event.current.mousePosition))
                {
                    if (Event.current.type == EventType.DragUpdated)
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        Event.current.Use();
                    }
                    else if (Event.current.type == EventType.DragPerform)
                    {
                        for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
                        {
                            var t = (DragAndDrop.objectReferences[i] as GameObject);
                            if (t != null)
                            {
                                ++rl.serializedProperty.arraySize;
                                rl.index = rl.serializedProperty.arraySize - 1;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("gameObject").objectReferenceValue = t;
                            }
                        }
                        Event.current.Use();
                    }
                }
            }



            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && rl.index >= 0)
            {
                list.DeleteArrayElementAtIndex(rl.index);
                if (rl.index >= list.arraySize - 1)
                {
                    rl.index = list.arraySize - 1;
                }
            }
        }

    }
}
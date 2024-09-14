using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Varia
{

    [CustomEditor(typeof(VariaRandomValue))]
    public class VariaRandomValueEditor : Editor
    {
        SerializedProperty list;

        ReorderableList rl;

        const int k_fieldPadding = 2;
        const int k_elementPadding = 5;
        const int k_thumbsHeight = 80;
        const int k_propertyHeight = 18;

        private bool useWeights;
        private bool showThumbs;
        private TargetPropertyEditor targetPropertyEditor;
        private float? propertyLabelWidth;
        private MethodInfo clearCacheMethodInfo;

        private float PropertyLabelWidth => propertyLabelWidth ?? (propertyLabelWidth = Math.Max(
            GUI.skin.label.CalcSize(targetPropertyEditor.PropertyLabel).x,
            GUI.skin.label.CalcSize(new GUIContent("Weight")).x
            )) ?? 0;

        private void AddNull()
        {
            var weightedValue = new VariaWeightedValue
            {
                value = VariaWeightedValue.GetDefault(targetPropertyEditor.PropertyType),
                weight = 1,
            };
            (target as VariaRandomValue).values.Add(weightedValue);
            serializedObject.Update();
        }

        /*
        private Type GetDrawerTypeForType(System.Type type)
        {
            var scriptAttributeUtilityType = typeof(EditorGUI).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
            return (Type)scriptAttributeUtilityType.GetMethod("GetDrawerTypeForType", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { type });
        }

        private Type GetDrawerTypeForPropertyAndType(SerializedProperty property,
      System.Type type)
        {
            var scriptAttributeUtilityType = typeof(EditorGUI).Assembly.GetType("UnityEditor.ScriptAttributeUtility");
            return (Type)scriptAttributeUtilityType.GetMethod("GetDrawerTypeForPropertyAndType", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { property, type });
        }
        */

        private void OnEnable()
        {
            list = serializedObject.FindProperty("values");
            list.isExpanded = true;

            targetPropertyEditor = new TargetPropertyEditor(serializedObject);
            targetPropertyEditor.OnPropertyTypeChange += OnPropertyTypeChange;
            targetPropertyEditor.OnPropertyChange += OnPropertyChange;

            rl = new ReorderableList(serializedObject, list, true, false, true, true);

            rl.headerHeight = 0;

            rl.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {

                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                if (targetElement.hasVisibleChildren)
                    rect.xMin += 10;
                var objectReferenceProperty = targetElement.FindPropertyRelative("objectReference");
                var valueProperty = targetElement.FindPropertyRelative("value");
                var weightProperty = targetElement.FindPropertyRelative("weight");

                var tileRect = rect;
                tileRect.y += k_fieldPadding;
                tileRect.height = showThumbs ? k_thumbsHeight : k_propertyHeight;
                var weightRect = rect;
                weightRect.yMin = tileRect.yMax + k_fieldPadding;
                weightRect.height = EditorGUI.GetPropertyHeight(weightProperty);

                var propertyType = targetPropertyEditor.PropertyType;
                var oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = PropertyLabelWidth;

                var weightedValue = (target as VariaRandomValue).values[index];

                void Field<T>(Func<T, T> fieldFunc)
                {
                    EditorGUI.BeginChangeCheck();
                    var oldValue = weightedValue.value is T ? (T)weightedValue.value : default(T);
                    var newValue = fieldFunc(oldValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        weightedValue.value = newValue;
                        serializedObject.Update();
                    }
                }

                var propertyLabel = targetPropertyEditor.PropertyLabel;

                if (propertyType == null)
                {
                    EditorGUI.LabelField(tileRect, $"Unknown property.");
                }
                else if (propertyType.IsSubclassOf(typeof(UnityEngine.Object)))
                {
                    objectReferenceProperty.objectReferenceValue = EditorGUI.ObjectField(tileRect, propertyLabel, objectReferenceProperty.objectReferenceValue, propertyType, true);
                    // TODO: Warn incompatible values?
                }
                else if (propertyType == typeof(int))
                {
                    Field<int>(f => EditorGUI.IntField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(bool))
                {
                    Field<bool>(f => EditorGUI.Toggle(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(float))
                {
                    Field<float>(f => EditorGUI.FloatField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(string))
                {
                    Field<string>(f => EditorGUI.TextField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(Color))
                {
                    Field<Color>(f => EditorGUI.ColorField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(LayerMask))
                {
                    Field<LayerMask>(f => EditorGUI.LayerField(tileRect, propertyLabel, f));
                }
                else if (propertyType.IsEnum)
                {
                    Field<int>(f => (int)(object)EditorGUI.EnumPopup(tileRect, propertyLabel, Enum.ToObject(propertyType, f) as Enum));
                }
                else if (propertyType == typeof(Vector2))
                {
                    Field<Vector2>(f => EditorGUI.Vector2Field(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(Vector3))
                {
                    Field<Vector3>(f => EditorGUI.Vector3Field(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(Vector4))
                {
                    Field<Vector4>(f => EditorGUI.Vector4Field(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(Rect))
                {
                    Field<Rect>(f => EditorGUI.RectField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(char))
                {
                    Field<char>(f =>
                    {
                        // Yep, this is what EditorGui.DefaultPropertyField does
                        var s = new string(f, 1);
                        s = EditorGUI.TextField(tileRect, propertyLabel, s);
                        if (s.Length == 1)
                            return s[0];

                        GUI.changed = false;
                        return ' ';
                    });
                }
                else if (propertyType == typeof(Bounds))
                {
                    Field<Bounds>(f => EditorGUI.BoundsField(tileRect, propertyLabel, f));
                }
                else if (propertyType == typeof(Quaternion))
                {
                    Field<Quaternion>(f => Quaternion.Euler(EditorGUI.Vector3Field(tileRect, propertyLabel, f.eulerAngles)));
                }
                else
                {
                    EditorGUI.LabelField(tileRect, $"Editor doesn't support {propertyType}");
                }

                if (useWeights)
                {
                    EditorGUI.PropertyField(weightRect, weightProperty);
                }
                EditorGUIUtility.labelWidth = oldLabelWidth;
            };

            /*
            rl.elementHeightCallback = (int index) =>
            {
                SerializedProperty targetElement = list.GetArrayElementAtIndex(index);
                var tileProperty = targetElement.FindPropertyRelative("objectReference");
                var tileHeight = showThumbs ? k_ThumbsHeight : EditorGUI.GetPropertyHeight(tileProperty);
                if (useWeights)
                {
                    var weightProperty = targetElement.FindPropertyRelative("weight");
                    return k_fieldPadding + tileHeight + k_fieldPadding + EditorGUI.GetPropertyHeight(weightProperty) + k_elementPadding;
                }
                else
                {
                    return k_fieldPadding + tileHeight + k_elementPadding;
                }
            };
            */

            rl.onAddCallback = l =>
            {
                AddNull();
                rl.index = rl.serializedProperty.arraySize - 1;
            };

            clearCacheMethodInfo = typeof(ReorderableList).GetMethod("ClearCache", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public void OnValidate()
        {
            if (targetPropertyEditor != null)
            {
                if (targetPropertyEditor.IsSet() && list.arraySize == 0)
                {
                    AddNull();
                }
            }
        }

        public override void OnInspectorGUI()
        {
            targetPropertyEditor.GUI();

            if (PlayerSettings.GetScriptingBackend(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget)) == ScriptingImplementation.IL2CPP)
            {
                EditorGUILayout.HelpBox($"Build target of {EditorUserBuildSettings.activeBuildTarget} is configured as Ahead-of-Time compilation.\n See docs on Varia Random Value for limitations of this component.", MessageType.Warning);
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("useWeights"));

            useWeights = serializedObject.FindProperty("useWeights").boolValue;
            if (EditorGUIUtility.HasObjectThumbnail(targetPropertyEditor.PropertyType))
            {
                showThumbs = EditorGUILayout.Toggle(new GUIContent("Show Thumbnails"), showThumbs);
            }
            else
            {
                showThumbs = false;
            }

            List();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));
            serializedObject.ApplyModifiedProperties();
        }

        private void List()
        {
            list.isExpanded = EditorGUILayout.Foldout(list.isExpanded, new GUIContent("Values"));

            if (list.isExpanded)
            {
                var r1 = GUILayoutUtility.GetLastRect();

                var elementHeight = k_fieldPadding + (showThumbs ? k_thumbsHeight : k_propertyHeight) + (useWeights ? k_fieldPadding + k_propertyHeight : 0) + k_elementPadding;
                if(rl.elementHeight != elementHeight)
                {
                    rl.elementHeight = elementHeight;
                    clearCacheMethodInfo?.Invoke(rl, new object[0]);
                }
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
                            var t = (DragAndDrop.objectReferences[i] as UnityEngine.Object);
                            if (t != null)
                            {
                                ++rl.serializedProperty.arraySize;
                                rl.index = rl.serializedProperty.arraySize - 1;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("weight").floatValue = 1.0f;
                                list.GetArrayElementAtIndex(rl.index).FindPropertyRelative("objectReference").objectReferenceValue = t;
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

        private void OnPropertyChange()
        {
            OnValidate();
            propertyLabelWidth = null;
        }

        private void OnPropertyTypeChange()
        {
            serializedObject.FindProperty("values").arraySize = 0;
            serializedObject.ApplyModifiedProperties();

            /*
            if (targetPropertyEditor.PropertyType != null)
            {
                var removeValues = (target as VariaRandomValue).values
                    .Select((w, i) => (w, i))
                    .Where(t => t.w.value is null || targetPropertyEditor.PropertyType.IsAssignableFrom(t.w.value.GetType()))
                    .Select(t => t.i)
                    .Reverse().ToList();

                var arrayProperty = serializedObject.FindProperty("values");
                foreach(var i in removeValues)
                {
                    arrayProperty.DeleteArrayElementAtIndex(i);
                }
                serializedObject.ApplyModifiedProperties();
            }
            */
        }
    }
}
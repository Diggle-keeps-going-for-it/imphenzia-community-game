using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Varia
{
    /// <summary>
    /// Gui for a pair of SerializedProperty covering:
    /// * GameObject target
    /// * string property
    /// 
    /// This pair can be used with VariaReflection to directly read/write any unity value.
    /// 
    /// There are several states that are relevant:
    /// * target null and property null or empty:
    ///   This pair is unset. target will default to SerializedObject.target if it is needed.
    /// * target non-null and property null or empty
    ///   this pair is unset
    /// * target non-null and property non-null and non-empty
    ///   this pair is set. The property expression may still be invalid 
    ///   (e.g. if the user deletes an existing component)
    ///   so you need to handle the possibility of VariaReflection.EvalExpression returning null.
    /// 
    /// (it should never be the case that target is null and property non-null)
    /// </summary>
    public class TargetPropertyEditor
    {
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty targetProperty;
        private readonly SerializedProperty propertyProperty;
        private readonly Func<VariaProperty, bool> propertyFilter;
        private readonly Func<MaterialProperty, bool> materialPropertyFilter;

        // For use with Gui
        private Rect objectButtonRect;
        private Rect propertyButtonRect;
        private GenericMenu.MenuFunction firstMenuItem;

        // Cached values
        private VariaMirror mirror;

        private Type propertyType;
        private GUIContent propertyLabel;

        public TargetPropertyEditor(
            SerializedObject serializedObject,
            SerializedProperty targetProperty = null,
            SerializedProperty propertyProperty = null,
            Func<VariaProperty, bool> propertyFilter = null,
            Func<MaterialProperty, bool> materialPropertyFilter = null
            )
        {
            this.serializedObject = serializedObject;

            this.targetProperty = targetProperty ?? serializedObject.FindProperty("target");

            this.propertyProperty = propertyProperty ?? serializedObject.FindProperty("property");
            this.propertyFilter = propertyFilter;
            this.materialPropertyFilter = materialPropertyFilter;
            RecalcProperty(false);
        }

        public void GUI()
        {
            var oldTarget = targetProperty.objectReferenceValue;
            EditorGUI.BeginChangeCheck();
            if (targetProperty.objectReferenceValue is null && string.IsNullOrEmpty(propertyProperty.stringValue))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(targetProperty.displayName);
                EditorGUILayout.LabelField("Self");
                if (GUILayout.Button("Edit target"))
                {
                    targetProperty.objectReferenceValue = (serializedObject.targetObject as Component).gameObject;
                    serializedObject.ApplyModifiedProperties();
                    return;
                }
                EditorGUILayout.EndHorizontal();
                //var content = EditorGUIUtility.ObjectContent(targetProperty.objectReferenceValue, targetProperty.objectReferenceValue.GetType()); ;
            }
            else
            {
                EditorGUILayout.ObjectField(targetProperty);
            }
            // If target has changed, do some validation
            if (EditorGUI.EndChangeCheck())
            {
                var target = targetProperty.objectReferenceValue;
                // Is the property expression still valid?
                var mirror = VariaReflection.EvalExpression(target?.GetType(), propertyProperty.stringValue);
                if (mirror == null)
                {
                    var targetGo = target is GameObject ? (GameObject)target : target is Component ? ((Component)target).gameObject : null;
                    UnityEngine.Object foundTarget = null;

                    // Cannot find property on target. Perhaps the user meant a different component?
                    // Check if there's a similar component around
                    if (oldTarget != null)
                    {
                        var oldType = oldTarget.GetType();
                        if (oldType == typeof(GameObject) && targetGo != null)
                        {
                            foundTarget = targetGo;
                        }
                        else if (typeof(Component).IsAssignableFrom(oldType) && targetGo != null)
                        {
                            foundTarget = targetGo.GetComponent(oldType);
                        }
                    }

                    // Are we now good?
                    mirror = VariaReflection.EvalExpression(foundTarget?.GetType(), propertyProperty.stringValue);
                    if (mirror != null)
                    {
                        targetProperty.objectReferenceValue = foundTarget;
                    }
                    else
                    {
                        // Couldn't find anything sensible, clear out property
                        propertyProperty.stringValue = null;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                RecalcProperty(true);
                OnPropertyChange?.Invoke();
            }

            var showPropertyProperty = true;

            if (showPropertyProperty)
            {
                GUILayout.BeginHorizontal();

                EditorGUILayout.PrefixLabel(propertyProperty.displayName);
                {
                    UnityEngine.GUI.enabled = !serializedObject.isEditingMultipleObjects;
                    if (GUILayout.Button(new GUIContent(propertyProperty.hasMultipleDifferentValues ? "(Multiple values)" : propertyProperty.stringValue), EditorStyles.popup))
                    {
                        var target = targetProperty.objectReferenceValue;
                        if (target == null) target = this.serializedObject.targetObject;
                        BuildPopupList(target, serializedObject, targetProperty, propertyProperty).DropDown(propertyButtonRect);
                    }
                    UnityEngine.GUI.enabled = true;
                    if (Event.current.type == EventType.Repaint)
                    {
                        propertyButtonRect = GUILayoutUtility.GetLastRect();
                    }
                }
                GUILayout.EndHorizontal();
            }
        }

        public bool IsSet()
        {
            return !string.IsNullOrEmpty(propertyProperty.stringValue);
        }

        public bool SetFirst()
        {
            var target = targetProperty.objectReferenceValue;
            if (target == null) target = this.serializedObject.targetObject;
            var menu = BuildPopupList(target, serializedObject, targetProperty, propertyProperty);
            if (firstMenuItem != null)
            {
                firstMenuItem();
                return true;
            }
            else
            {
                return false;
            }
        }

        public Action OnPropertyTypeChange;
        public Action OnPropertyChange;

        public VariaMirror Mirror => mirror;
        public Type PropertyType => propertyType;
        public GUIContent PropertyLabel => propertyLabel;

        private GenericMenu BuildPopupList(
          UnityEngine.Object target,
          SerializedObject serializedObject,
          SerializedProperty targetProperty,
          SerializedProperty propertyProperty)
        {
            firstMenuItem = null;

            if (target is Component)
                target = (target as Component).gameObject;
            var menu = new GenericMenu();
            if (target == null)
            {
                menu.AddDisabledItem(new GUIContent($"No target selected"));
            }

            // Popup entries for fields of the target itself
            GeneratePopUpForType(menu, target, false, false, serializedObject, targetProperty, propertyProperty);


            if (target is GameObject)
            {
                // Popup entries for fields of each component
                var components = (target as GameObject).GetComponents<Component>();
                List<string> repeatedTypes = components.Where(c => c != null).Select(c => c.GetType().Name).GroupBy(x => x).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                foreach (Component component in components)
                {
                    if (component != null && !component.GetType().IsSubclassOf(typeof(VariaBehaviour)))
                    {
                        GeneratePopUpForType(menu, component, repeatedTypes.Contains(component.GetType().Name), true, serializedObject, targetProperty, propertyProperty);
                    }
                    if (component is Renderer r)
                    {
                        for (var materialIndex = 0; materialIndex < r.sharedMaterials.Length; materialIndex++)
                        {
                            var localMaterialIndex = materialIndex;
                            var material = r.sharedMaterials[materialIndex];
                            if (material != null)
                            {
                                foreach (var p in MaterialEditor.GetMaterialProperties(new[] { material }))
                                {
                                    if (p.flags.HasFlag(MaterialProperty.PropFlags.HideInInspector))
                                        continue;
                                    if (materialPropertyFilter?.Invoke(p) == false)
                                        continue;
                                    // TODO: Only show PerRendererData properties?
                                    {
                                        var menuPath = $"Material {materialIndex}/{p.displayName}";
                                        GenericMenu.MenuFunction menuFunc = () =>
                                        {
                                            targetProperty.objectReferenceValue = component;
                                            propertyProperty.stringValue = VariaEditorReflection.GetMaterialExpression(localMaterialIndex, p);
                                            serializedObject.ApplyModifiedProperties();
                                            RecalcProperty(true);
                                            OnPropertyChange?.Invoke();
                                        };
                                        menu.AddItem(new GUIContent(menuPath), false, menuFunc);
                                    }

                                    {
                                        var menuPath = $"Material {materialIndex} Property Block/{p.displayName}";
                                        GenericMenu.MenuFunction menuFunc = () =>
                                        {
                                            targetProperty.objectReferenceValue = component;
                                            propertyProperty.stringValue = VariaEditorReflection.GetPropertyBlockExpression(localMaterialIndex, p);
                                            serializedObject.ApplyModifiedProperties();
                                            RecalcProperty(true);
                                            OnPropertyChange?.Invoke();
                                        };
                                        firstMenuItem = firstMenuItem ?? menuFunc;
                                        menu.AddItem(new GUIContent(menuPath), false, menuFunc);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if(firstMenuItem == null)
            {
                menu.AddDisabledItem(new GUIContent($"No appropriate properties found on {target.name}"));
            }

            return menu;
        }

        private void GeneratePopUpForType(
            GenericMenu menu,
            UnityEngine.Object target,
            bool useFullTargetName,
            bool isComponent,
            SerializedObject serializedObject,
            SerializedProperty targetProperty,
            SerializedProperty propertyProperty)
        {
            string targetName = useFullTargetName ? target.GetType().FullName : target.GetType().Name;
            var allProperties = VariaReflection.GetProperties(target.GetType());
            for (var i = 0; i < allProperties.Count; i++)
            {
                var prop = allProperties[i];
                // Some properties are redundant with GameObject
                if (isComponent && (prop.name == "tag" || prop.name == "name"))
                {
                    continue;
                }
                if (!IsValidProperty(prop))
                    continue;
                if (propertyFilter?.Invoke(prop) == false)
                    continue;

                var menuPath = targetName + "/" + prop.expression.Replace(".", "/");
                // If we have menu paths "a/b" and "a/b/c", rewrite the former as "a/b/[typename]"
                // as otherwise the menus look very weird.
                var nextExpression = i < allProperties.Count - 1 ? allProperties[i + 1].expression : "";
                if (nextExpression.StartsWith(prop.expression + "."))
                {
                    menuPath += "/" + prop.propertyType.Name;
                }

                GenericMenu.MenuFunction menuFunc = () =>
                {
                    targetProperty.objectReferenceValue = target;
                    propertyProperty.stringValue = prop.expression;
                    serializedObject.ApplyModifiedProperties();
                    RecalcProperty(true);
                    OnPropertyChange?.Invoke();
                };
                firstMenuItem = firstMenuItem ?? menuFunc;
                menu.AddItem(new GUIContent(menuPath), false, menuFunc);
            }
        }

        private static bool IsValidProperty(VariaProperty prop)
        {
            return prop.canRead && prop.canWrite && VariaWeightedValue.CanSerialize(prop.propertyType);
        }

        private void RecalcProperty(bool canUpdate)
        {
            mirror = string.IsNullOrEmpty(propertyProperty.stringValue) ? null : VariaReflection.EvalExpression(targetProperty.objectReferenceValue?.GetType(), propertyProperty.stringValue);

            var oldPropertyType = propertyType;
            propertyType = mirror?.propertyType;
            if (propertyType == typeof(float))
            {
                propertyLabel = new GUIContent("float");
            }
            else
            {
                propertyLabel = new GUIContent(propertyType?.Name);
            }
            if (canUpdate && oldPropertyType != propertyType)
            {
                OnPropertyTypeChange?.Invoke();
            }
        }

    }
}

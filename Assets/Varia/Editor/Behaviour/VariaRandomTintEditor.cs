using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Varia
{
    [CustomEditor(typeof(VariaRandomTint))]
    [CanEditMultipleObjects]
    public class VariaRandomTintEditor : Editor
    {
        private TargetPropertyEditor targetPropertyEditor;

        public void OnEnable()
        {
            targetPropertyEditor = new TargetPropertyEditor(serializedObject,
                propertyFilter: p => p.propertyType == typeof(Color),
                materialPropertyFilter: p => p.type == MaterialProperty.PropType.Color);
        }

        public override void OnInspectorGUI()
        {
            var t = target as VariaRandomTint;
            var min = t.relative ? -1 : 0;
            var max = t.relative ? 2 : 1;
            serializedObject.Update();
            targetPropertyEditor.GUI();
            if (!targetPropertyEditor.IsSet())
            {
                if(targetPropertyEditor.SetFirst())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                else
                {
                    EditorGUILayout.HelpBox("No component found with an appropriate Color property. Add a Renderer.", MessageType.Warning);
                }
            }
            if (t.property?.StartsWith("material") == true && t.target is Renderer)
            {
                EditorGUILayout.HelpBox("MeshRenderer Tint does not preview in the editor.", MessageType.Warning);
            }
            var wasRelative = serializedObject.FindProperty("relative").boolValue;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("relative"));
            var isRelative = serializedObject.FindProperty("relative").boolValue;
            if (isRelative)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("relativeParent"));
            }
            if (wasRelative && !isRelative)
            {
                serializedObject.FindProperty("saturationMin").floatValue += 1;
                serializedObject.FindProperty("saturationMax").floatValue += 1;
                serializedObject.FindProperty("valueMin").floatValue += 1;
                serializedObject.FindProperty("valueMax").floatValue += 1;
            }
            if (!wasRelative && isRelative)
            {
                serializedObject.FindProperty("saturationMin").floatValue -= 1;
                serializedObject.FindProperty("saturationMax").floatValue -= 1;
                serializedObject.FindProperty("valueMin").floatValue -= 1;
                serializedObject.FindProperty("valueMax").floatValue -= 1;
            }

            EditorUtility.NiceRange(serializedObject, "hueMin", "hueMax", min, 1, "Hue");
            EditorUtility.NiceRange(serializedObject, "saturationMin", "saturationMax", min, 1, "Saturation");
            EditorUtility.NiceRange(serializedObject, "valueMin", "valueMax", min, 1, "Value");
            EditorUtility.NiceRange(serializedObject, "alphaMin", "alphaMax", 0, max, "Alpha");


            EditorGUILayout.PropertyField(serializedObject.FindProperty("conditionList"));

            serializedObject.ApplyModifiedProperties();

            if (t != null)
            {
                //var colors = Enumerable.Range(0, 20).Select(_ => t.GetColor()).Cast<Color>().ToArray();
                var colors = Enumerable.Range(0, 20).Select(_ => Color.red).Cast<Color>().ToArray();
                DrawColors(20, () => t.GetColor(true).Value);
            }
        }

        public void DrawColors(int count, System.Func<Color> getColor)
        {
            // TODO: This try block shouldn't be so catch all.
            try
            {
                var t = target as VariaRandomTint;

                if (t.relative && t.GetBaseColor() == null)
                {
                    EditorGUILayout.LabelField("Example colors (relative to white)", EditorStyles.boldLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("Example colors", EditorStyles.boldLabel);
                }

                var r = EditorGUILayout.BeginVertical();

                //r.width = EditorGUIUtility.currentViewWidth;
                r.width = (float)typeof(EditorGUIUtility).GetProperty("contextWidth", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null, null) - 40;


                var boxStyle = GUI.skin.box;
                var tileSize = 20;

                var boxRect = boxStyle.margin.Remove(r);
                var innerRect = boxStyle.padding.Remove(boxRect);

                var tilesPerRow = (int)(innerRect.width / tileSize);
                var oldState = Random.state;
                Random.InitState(1);
                if (tilesPerRow > 0)
                {
                    var rows = (count + tilesPerRow - 1) / tilesPerRow;
                    innerRect.height = rows * tileSize;
                    boxRect = boxStyle.padding.Add(innerRect);

                    GUI.Box(boxRect, "");

                    for (var i = 0; i < rows * tilesPerRow; i++)
                    {
                        var x = innerRect.x + (i % tilesPerRow) * tileSize;
                        var y = innerRect.y + (i / tilesPerRow) * tileSize;
                        var c = getColor();
                        if (Event.current.type == EventType.Repaint)
                        {
                            var tileRect = new Rect(x, y, tileSize, tileSize);
                            var texture = MakeTexture(1, 1, c);
                            GUI.DrawTexture(tileRect, texture);
                        }
                    }
                }
                EditorGUILayout.EndVertical();
                Random.state = oldState;

                GUILayout.Space(boxStyle.margin.Add(boxRect).height);

            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox(e.Message, MessageType.Error);
            }
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var texture = new Texture2D(width, height);
            Color[] pixels = Enumerable.Repeat(color, width * height).ToArray();
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
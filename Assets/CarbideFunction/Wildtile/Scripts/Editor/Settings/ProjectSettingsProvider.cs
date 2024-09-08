using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class ProjectSettingsProvider
    {
        private const float labelWidth = 300f;

        private static bool isInstallerExpanded = false;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Wildtile", SettingsScope.Project)
            {
                label = "Wildtile",

                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth = labelWidth;

                    var settings = ProjectSettings.instance;

                    EditorGUI.BeginChangeCheck();

                    isInstallerExpanded = EditorGUILayout.BeginFoldoutHeaderGroup(isInstallerExpanded, "Installer");
                    if (isInstallerExpanded)
                    {
                        settings.installer.OnInspectorGUI();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.Save();
                    }
                },

                keywords = new HashSet<string>(new[] { "Wave Function Collapse", "WaveFunctionCollapse", "WFC" })
            };
        }
    }
}

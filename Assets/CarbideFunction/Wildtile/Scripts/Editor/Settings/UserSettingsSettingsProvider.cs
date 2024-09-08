using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class UserSettingsSettingsProvider
    {
        private const float labelWidth = 300f;

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Preferences/Wildtile", SettingsScope.User)
            {
                label = "Wildtile",

                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth = labelWidth;

                    var settings = UserSettings.instance;

                    EditorGUI.BeginChangeCheck();

                    settings.maximumGridPlacerSecondsPerFrameInEditor = EditorGUILayout.FloatField(
                        new GUIContent(
                            ObjectNames.NicifyVariableName(nameof(UserSettings.maximumGridPlacerSecondsPerFrameInEditor)),
                            $"This value controls how much time will be spent per frame when generating grids using the {nameof(GridPlacer)} in editor only. To control the in-game time, edit the \"{ObjectNames.NicifyVariableName(nameof(GridPlacer.maxInGameSecondsPerFrame))}\" property on the {nameof(GridPlacer)}.\n\nIncreasing this value will slightly reduce the amount of time you need to wait for the {nameof(GridPlacer)} to generate the mesh. Reducing this value will improve editor framerate while waiting for the {nameof(GridPlacer)} to generate the mesh."
                        ),
                        settings.maximumGridPlacerSecondsPerFrameInEditor
                    );

                    settings.tilesetInspectorSettings.OnGui();

                    if (EditorGUI.EndChangeCheck())
                    {
                        settings.Save();
                    }

                    EditorGUIUtility.labelWidth = 0;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Wave Function Collapse", "WaveFunctionCollapse", "WFC", "Logging" })
            };
        }

        [SettingsProvider]
        public static SettingsProvider CreateDebugSettingsProvider()
        {
            return new SettingsProvider("Preferences/Wildtile/Debug", SettingsScope.User)
            {
                label = "Debug",

                // Create the SettingsProvider and initialize its drawing (IMGUI) function in place:
                guiHandler = (searchContext) =>
                {
                    EditorGUIUtility.labelWidth = labelWidth;

                    var settings = UserSettings.instance;
                    var transientSettings = TransientUserSettings.instance;

                    transientSettings.importLoggingEnabled = EditorGUILayout.Toggle("Import logging", transientSettings.importLoggingEnabled);

                    settings.rethrowImportExceptions = EditorGUILayout.Toggle(
                        new GUIContent(
                            ObjectNames.NicifyVariableName(nameof(UserSettings.rethrowImportExceptions)),
                            $"If any exceptions occur while importing Wildtile will report them in the Tileset Import Report window. If this setting is checked, they will also be reported in the console window, along with other information such as error location. This is useful for debugging the importer, but may introduce issues with Unity's UI."
                        ),
                        settings.rethrowImportExceptions
                    );

                    settings.showTilesetInspectorStageObjects = EditorGUILayout.Toggle(
                        new GUIContent(
                            ObjectNames.NicifyVariableName(nameof(UserSettings.showTilesetInspectorStageObjects)),
                            $"When creating objects in the Tileset Inspector stage (the scene shown after double clicking on a module), choose whether to show the objects in the hierarchy view and make them pickable in the scene view. By hiding them, it becomes clearer when you are in inspector mode and you will not make edits that are then discarded.\n" +
                            "\n" +
                            "Changing this setting will take effect when you select a new module in the list"
                        ),
                        settings.showTilesetInspectorStageObjects
                    );

                    EditorGUIUtility.labelWidth = 0;
                },

                // Populate the search keywords to enable smart search filtering and label highlighting:
                keywords = new HashSet<string>(new[] { "Wave Function Collapse", "WaveFunctionCollapse", "WFC", "Logging", "Debug" })
            };
        }
    }
}

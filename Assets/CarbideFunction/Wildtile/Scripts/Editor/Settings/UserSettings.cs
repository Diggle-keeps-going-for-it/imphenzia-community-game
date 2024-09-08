using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    [FilePath("Wildtile/UserSettings.asset", FilePathAttribute.Location.PreferencesFolder)]
    [InitializeOnLoad]
    internal class UserSettings : ScriptableSingleton<UserSettings>
    {
        [SerializeField]
        public float maximumGridPlacerSecondsPerFrameInEditor = 0.005f;

        [SerializeField]
        public TilesetInspectorSettings tilesetInspectorSettings = new TilesetInspectorSettings();

        [SerializeField]
        public bool rethrowImportExceptions = false;

        [SerializeField]
        public bool showTilesetInspectorStageObjects = false;

        public void Save()
        {
            base.Save(true);
            loggingEnabledChanged?.Invoke();
        }

        public delegate void LoggingEnabledChanged();
        public LoggingEnabledChanged loggingEnabledChanged;

        static UserSettings()
        {
            // this loads the settings from the file on disk
            EditorApplication.delayCall += () => TouchInstance(instance);
        }

        private static void TouchInstance(UserSettings userSettings)
        {
            // intentionally empty - the call touches the instance and forces it to load
        }

        private void OnEnable()
        {
            CarbideFunction.Wildtile.GridPlacer.getEditorMaxSecondsPerFrame = () => maximumGridPlacerSecondsPerFrameInEditor;
        }
    }

    /// <summary>
    /// Settings in this class will be reset when the user shuts down the Unity Editor.
    ///
    /// This is useful for settings that can cause extreme log-spam, which slows the editor down and consumes a lot of memory and in extreme cases crashes the user's computer.
    /// </summary>
    [InitializeOnLoad]
    internal class TransientUserSettings : ScriptableSingleton<TransientUserSettings>
    {
        [SerializeField]
        public bool importLoggingEnabled = false;

        private void OnEnable()
        {
            ImportInfoLogSettingsGetter.getIsEnabled = () => importLoggingEnabled;
        }

        static TransientUserSettings()
        {
            // this loads the settings from the file on disk
            EditorApplication.delayCall += () => TouchInstance(instance);
        }

        private static void TouchInstance(TransientUserSettings userSettings)
        {
            // intentionally empty - the call touches the instance
        }
    }
}

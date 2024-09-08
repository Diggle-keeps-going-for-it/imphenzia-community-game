using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
internal interface IIsLogEnabledGetter
{
#if UNITY_EDITOR
    bool IsEnabled();
#endif
}


/// <summary>
/// Handles log messages without costing lots of work if the logs are disabled by the user in their user preferences.
/// 
/// Example implementation of <paramref name="SettingGetter"/>:
/// <code>
/// public struct CustomLogGetter : IIsLogEnabledGetter
/// {
///     bool IIsLogEnabledGetter.IsEnabled(Editor.UserSettings userSettings)
///     {
///         // put your custom log setting here
///         return userSettings.reportCustomLogs;
///     }
/// }
/// </code>
/// </summary>
internal class GenericInfoLog<SettingsGetter> where SettingsGetter : IIsLogEnabledGetter, new()
{
    public delegate string GenerateMessage();
    [Conditional("UNITY_EDITOR")]
    public static void Log(GenerateMessage generateMessage)
    {
#if UNITY_EDITOR
        if (ShouldBeEnabled())
        {
            logger.Log(generateMessage());
        }
#endif
    }

#if UNITY_EDITOR
    private static UnityEngine.Logger logger = CreateLogger();

    private static UnityEngine.Logger CreateLogger()
    {
        var createdLogger = new UnityEngine.Logger(UnityEngine.Debug.unityLogger.logHandler);
        return createdLogger;
    }

    private static bool ShouldBeEnabled()
    {
        var settingsGetterInstance = new SettingsGetter();
        return settingsGetterInstance.IsEnabled();
    }
#endif
}
}

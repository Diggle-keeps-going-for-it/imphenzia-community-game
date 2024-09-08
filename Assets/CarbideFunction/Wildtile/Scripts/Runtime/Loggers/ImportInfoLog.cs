using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{
internal struct ImportInfoLogSettingsGetter : IIsLogEnabledGetter
{
#if UNITY_EDITOR
    bool IIsLogEnabledGetter.IsEnabled()
    {
        return getIsEnabled != null ? getIsEnabled() : false;
    }

    public delegate bool GetIsEnabled();
    public static GetIsEnabled getIsEnabled;
#endif
}

internal class ImportInfoLog : GenericInfoLog<ImportInfoLogSettingsGetter>
{
}
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.ImportReport
{

/// <summary>
/// Holds a <see cref="TilesetReportWindowParameters"/> during edit-mode. This property should be set by clicking on this script and assigning an asset.
///
/// This allows editor classes to access the search parameters even if they do not have reliable default serialized property loading (e.g. <see href="https://docs.unity3d.com/ScriptReference/EditorWindow.html">EditorWindows</see>).
/// </summary>
internal class TilesetReportWindowParametersAccess : ScriptableSingleton<TilesetReportWindowParametersAccess>
{
    [SerializeField] private TilesetReportWindowParameters parameters = null;
    public TilesetReportWindowParameters Parameters => parameters;
}

}

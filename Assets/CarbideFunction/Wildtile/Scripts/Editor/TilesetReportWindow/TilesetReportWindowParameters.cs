using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.ImportReport
{

/// <summary>
/// Editor only resource to configure how the Tileset Report Window should act.
///
/// Intended for use with <see cref="TilesetReportWindow"/>.
/// </summary>
// uncomment to create your own, but shouldn't be necessary 
[CreateAssetMenu(menuName=MenuConstants.topMenuName + "Tileset Report Window Parameters", order = MenuConstants.orderBase + 11, fileName="New Tileset Report Window Parameters")]
internal class TilesetReportWindowParameters : ScriptableObject
{
    /// <summary>
    /// A reference to an asset that defines how the missing tiles should be searched and interacted with.
    /// </summary>
    public MissingTilesParameters missingTileParameters = null;

    /// <summary>
    /// Data to configure the hash collision sub window inside the <see cref="TilesetReportWindow"/>.
    /// </summary>
    public ImportReport.SubWindow.HashCollisionSubWindow.Data hashCollisionSubWindowData = new ImportReport.SubWindow.HashCollisionSubWindow.Data();
}

}

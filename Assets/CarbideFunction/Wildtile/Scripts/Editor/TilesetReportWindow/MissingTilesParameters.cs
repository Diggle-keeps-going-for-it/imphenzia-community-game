using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Backend resource to define the subset of marching cube indices that can be rotated and flipped to create the full 256 marching cube indices.
/// This also includes example models for each marching cube index to show which tiles should be filled.
///
/// Intended for use with <see cref="CarbideFunction.Wildtile.Editor.ImportReport.SubWindow.MissingTilesSubWindow"/>.
/// </summary>
// uncomment to create your own, but shouldn't be necessary 
// [CreateAssetMenu(menuName=MenuConstants.topMenuName + "Missing Tiles Search Parameters", order = MenuConstants.orderBase + 11, fileName="New Missing Tiles Search Parameters")]
internal class MissingTilesParameters : ScriptableObject
{

    /// <summary>
    /// 55 unique marching cubes configurations to search for.
    /// </summary>
    public List<SearchConfiguration> searchConfigurations = new List<SearchConfiguration>();

    /// <summary>
    /// Rendering objects (e.g. materials) used to render the raw meshes in <see cref="searchConfigurations"/>.
    /// </summary>
    public ImportReport.SubWindow.MissingTilesSubWindow.Data renderingData = new ImportReport.SubWindow.MissingTilesSubWindow.Data();
    /// <summary>
    /// Scalar applied to the user's mouse input when they click and drag on the preview window.
    /// </summary>
    public Vector2 mouseOrbitSpeed = Vector2.one * -0.5f;
}

}

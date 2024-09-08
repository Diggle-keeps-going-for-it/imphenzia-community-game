using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile.Postprocessing;

namespace CarbideFunction.Wildtile.PlacerPostprocessing
{

/// <summary>
/// Factory used to create <see cref="PlacerPostprocessor"/> assets for tilesets.
/// </summary>
[CreateAssetMenu(menuName = MenuConstants.topMenuName + "Placer Postprocessor Creator", fileName="New Wildtile PlacerPostprocessor Creator", order = MenuConstants.orderBase + 4)]
public class PlacerPostprocessorCreator : PostprocessorCreator
{
    public override Postprocessor CreatePostprocessor(ImporterSettings.Settings importSettings)
    {
        var postprocessor = ScriptableObject.CreateInstance<PlacerPostprocessor>();
        return postprocessor;
    }
}

}

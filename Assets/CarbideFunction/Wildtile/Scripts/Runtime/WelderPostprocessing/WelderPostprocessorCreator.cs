using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CarbideFunction.Wildtile.Postprocessing;

namespace CarbideFunction.Wildtile.WelderPostprocessing
{

/// <summary>
/// Factory used to create <see cref="WelderPostprocessor"/> assets for tilesets.
/// </summary>
[CreateAssetMenu(menuName = MenuConstants.topMenuName + "Welder Postprocessor Creator", fileName="New Wildtile WelderPostprocessor Creator", order = MenuConstants.orderBase + 5)]
public class WelderPostprocessorCreator : PostprocessorCreator
{
    [SerializeField]
    private float weldTolerance = 1E-4f;

    public override Postprocessor CreatePostprocessor(
        ImporterSettings.Settings importSettings
    )
    {
        var postprocessor = ScriptableObject.CreateInstance<WelderPostprocessor>();
        postprocessor.weldToleranceSquare = weldTolerance * weldTolerance;
        postprocessor.manifoldMaterials = importSettings.materialImportSettings.Where(settings => settings.isPartOfManifoldMesh).Select(settings => settings.targetMaterial).ToList();
        return postprocessor;
    }
}

}

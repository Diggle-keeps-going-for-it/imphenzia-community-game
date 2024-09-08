using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.ImporterSettings;
using CarbideFunction.Wildtile.Postprocessing;

[CreateAssetMenu(menuName = MenuConstants.topMenuName + "Examples/Postprocessor Creator", fileName="New Level Editor Postprocessor Creator")]
public class LevelEditorPostProcessorCreator : PostprocessorCreator
{
    public override Postprocessor CreatePostprocessor(Settings importSettings)
    {
        var postprocessor = ScriptableObject.CreateInstance<LevelEditorPostProcessor>();
        return postprocessor;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Postprocessing
{

/// <summary>
/// Factory for postprocessors.
/// </summary>
public abstract class PostprocessorCreator : ScriptableObject
{
    /// <summary>
    /// This will be called each time the tileset is imported. You can implement this abstract method to introduce tileset-specific rules.
    /// </summary>
    public abstract Postprocessor CreatePostprocessor(ImporterSettings.Settings importerSettings);
}

}


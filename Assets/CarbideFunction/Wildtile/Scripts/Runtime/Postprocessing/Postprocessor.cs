using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Postprocessing
{

/// <summary>
/// Wildtile passes the postprocessor C#-only modules, and the postprocessor creates Unity objects.
///
/// Different postprocessor implementations can create Unity objects in different ways. For example: one postprocessor may create prefabs, transform them as needed. Another postprocessor may read the models from the module prefabs and generate a new mesh from all the modules.
///
/// Postprocessors will be replaced in the future with a simpler system. A replacement for <see cref="GridPlacer"/> will be provided to support welding and prop placement.
/// </summary>
public abstract class Postprocessor : ScriptableObject
{
    /// <summary>
    /// Convert a C#-only map into Unity objects. This is called after <see cref="GridPlacer"/> finishes collapsing the map. `GridPlacer` does not place objects itself. Instead, it uses the tileset's `Postprocessor.Postprocess()` to spawn in the necessary objects.
    /// </summary>
    public abstract IEnumerable<int> Postprocess(GameObject root, PostprocessableMap map, Vector3 tileDimensions);

    /// <summary>
    /// Clone an instantiated map and return the cloned map. Used when the user clicks on the <see cref="GridPlacer"/> &#8594; "Duplicate to Permanent Instance" button.
    /// </summary>
    public virtual UnityEngine.Object[] CloneAndReferenceTransientObjects(GameObject rootOfAlreadyPostprocessedWorld)
    {
        return new UnityEngine.Object[]{};
    }
}

}


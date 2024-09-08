using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.ImporterSettings
{

[Serializable]
public class Settings
{
    [Tooltip("The width of the tiles in the XZ dimensions")]
    public float tileWidth = 1f;
    [Tooltip("The height of the tiles in the Y dimension")]
    public float tileHeight = 1f;
    public Vector3 TileDimensions => new Vector3(tileWidth, tileHeight, tileWidth);

    [Tooltip("How precisely should the hashing algorithm treat vertex positions? When working out which modules are connected to one another, a larger value will lead to fewer false positives and more false negatives. A smaller value will lead to fewer false negatives and more false positives.")]
    [Min(1)]
    public int positionHashResolution = 64;

    [Tooltip("How precisely should the hashing algorithm treat vertex normals? When working out which modules are connected to one another, a larger value will lead to fewer false positives and more false negatives. A smaller value will lead to fewer false negatives and more false positives.")]
    [Min(1)]
    public int normalHashResolution = 64;

    [Tooltip("Import settings for the different materials used by the mesh.")]
    public List<MaterialImportSettings> materialImportSettings = new List<MaterialImportSettings>();
}

}

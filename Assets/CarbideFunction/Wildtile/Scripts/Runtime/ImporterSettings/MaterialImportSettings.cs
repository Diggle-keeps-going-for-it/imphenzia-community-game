using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.ImporterSettings
{

[Serializable]
public class MaterialImportSettings
{
    [SerializeField]
    public Material targetMaterial = null;

    [SerializeField]
    [Tooltip("Do faces with this material make up the closed surface of tiles (along with other manifold materials)? Used for determining if a cube corner is filled or empty. This material should not be two-sided.\n\nIf no materials are marked as manifold then all materials are treated as manifold materials.")]
    public bool isPartOfManifoldMesh = false;

    [SerializeField]
    [Tooltip("If true, if this material spans a tile border then the corresponding face must have the same vertex normals. If false, the corresponding face will match even if it is angled differently.")]
    public bool mustMatchNormalsOnBorder = false;

    [SerializeField]
    [Tooltip("If false, faces with this material will be ignored by the mesh matching algorithm.")]
    public bool mustMatch = true;

    public static readonly MaterialImportSettings defaultSettings = new MaterialImportSettings();
}

}

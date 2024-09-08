using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

/// <summary>
/// Backend resource to access the resources for highlighting triangles that lie on modules' bounding boxes.
///
/// It's intended for you to use script defaults within Unity to set this reference to an asset in your assets folder.
///
/// Intended for use with <see cref="TrianglesOnModuleFaceDrawer"/>.
/// </summary>
internal class TrianglesOnModuleFaceDrawerResourcesAccess : ScriptableSingleton<TrianglesOnModuleFaceDrawerResourcesAccess>
{
    public TrianglesOnModuleFaceDrawerResources resources;
}

}

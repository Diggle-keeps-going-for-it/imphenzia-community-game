using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

/// <summary>
/// Backend resource to define the resources for highlighting triangles that lie on modules' bounding boxes.
///
/// Intended for use with <see cref="TrianglesOnModuleFaceDrawer"/>.
/// </summary>
// Uncomment the next line to allow user-construction of this asset
//[CreateAssetMenu(menuName=MenuConstants.topMenuName + "Triangles on Module Face Drawer Resources", order = MenuConstants.orderBase + 11, fileName="New Triangles on Module Face Drawer Resources")]
internal class TrianglesOnModuleFaceDrawerResources : ScriptableObject
{
    public Material triangleOnFaceMaterial;
    public Material borderHighlightMaterial;

    public string borderWidthSliderName = "border-width";
    public string normalOffsetSliderName = "normal-offset";
}

}

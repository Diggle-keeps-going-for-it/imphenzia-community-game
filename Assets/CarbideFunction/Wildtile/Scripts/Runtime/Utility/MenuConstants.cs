using System;
using UnityEngine;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Contains constants for adding Wildtile specific options to Unity's different menus.
///
/// Designed for use with <see href="https://docs.unity3d.com/ScriptReference/CreateAssetMenuAttribute.html">CreateAssetMenuAttribute</see>, <see href="https://docs.unity3d.com/ScriptReference/AddComponentMenu.html">AddComponentMenu</see>, and <see href="https://docs.unity3d.com/ScriptReference/MenuItem.html">MenuItem</see>.
/// </summary>
public static class MenuConstants
{
    /// <summary>
    /// The base order value for <see href="https://docs.unity3d.com/ScriptReference/CreateAssetMenuAttribute.html">CreateAssetMenuAttribute</see> and <see href="https://docs.unity3d.com/ScriptReference/MenuItem.html">MenuItem</see>.
    /// </summary>
    /// <example>
    /// <code>
    /// [CreateAssetMenu(
    ///     fileName="New Wildtile Asset",
    ///     menuName=MenuConstants.topMenuName + "Wildtile Asset",
    ///     order=MenuConstants.orderBase + 5
    /// )]
    /// class WildtileAsset : ScriptableObject
    /// {
    /// }
    /// </code>
    /// </example>
    public const int orderBase = 1897; // random number below the bottom of the "right click on project window" -> "Create" menu

    /// <summary>
    /// The root menu name for <see href="https://docs.unity3d.com/ScriptReference/CreateAssetMenuAttribute.html">CreateAssetMenuAttribute</see> and <see href="https://docs.unity3d.com/ScriptReference/MenuItem.html">MenuItem</see>.
    ///
    /// This includes the path separator, so any added values should start with characters immediately:
    /// <c>MenuConstants.topMenuName + "My Custom Asset"</c>
    /// </summary>
    /// <example>
    /// <code>
    /// [CreateAssetMenu(
    ///     fileName="New Wildtile Asset",
    ///     menuName=MenuConstants.topMenuName + "Wildtile Asset",
    /// )]
    /// class WildtileAsset : ScriptableObject
    /// {
    /// }
    /// </code>
    /// </example>
    public const string topMenuName = "Wildtile/";

    /// <summary>
    /// The base order value for <see href="https://docs.unity3d.com/ScriptReference/AddComponentMenu.html">AddComponentMenu</see>.
    /// </summary>
    /// <example>
    /// <code>
    /// [AddComponentMenu(
    ///     MenuConstants.topMenuName + "Wildtile Component",
    ///     componentOrder=MenuConstants.addComponentOrderBase + 5
    /// )]
    /// class WildtileComponent : MonoBehaviour
    /// {
    /// }
    /// </code>
    /// </example>
    public const int addComponentOrderBase = 1897; // random number below the bottom of the "right click on project window" -> "Create" menu
}

}

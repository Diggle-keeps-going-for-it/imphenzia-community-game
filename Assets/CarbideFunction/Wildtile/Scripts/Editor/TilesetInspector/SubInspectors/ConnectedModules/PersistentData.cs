using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
[Serializable]
internal class PersistentData
{
    public Face direction = Face.Forward;

    public enum ModuleValidity
    {
        ValidOnly,
        MatchingVertexContents,
        All,
    }
    public readonly static string[] moduleValidityModeStringToValue = new string[]{
        "Valid Only",
        "Matching Vertex Contents",
        "All",
    };
    public ModuleValidity moduleValidity = ModuleValidity.ValidOnly;

    [SerializeField]
    public SubInspectors.ConnectedModules.CachedConnectedModuleIdentifier cachedConnectedModule = null;
}
}

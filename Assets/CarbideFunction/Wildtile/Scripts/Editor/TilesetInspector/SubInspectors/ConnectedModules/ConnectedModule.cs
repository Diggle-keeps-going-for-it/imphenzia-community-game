using System;
using UnityEngine;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
internal class ConnectedModule
{
    public TransformedModule transformedModule;
    public enum ModuleValidity
    {
        Valid,
        MatchingContentsButMismatchingFaces,
        MismatchingContents,
    }
    public ModuleValidity validity;
}
}

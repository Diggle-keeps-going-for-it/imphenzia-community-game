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
internal class CachedConnectedModuleIdentifier
{
    public string prefabName;
    public int yawIndex;
    public bool isFlipped;
}
}

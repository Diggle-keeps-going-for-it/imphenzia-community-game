using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
internal class View
{
    public View(VisualElement rootElement)
    {
        directionField = rootElement.Query<DropdownField>("direction").First();
        validityField = rootElement.Query<DropdownField>("element-validity").First();
        connectedModulesField = rootElement.Query<ListView>("connected-modules").First();
    }

    internal readonly DropdownField directionField = null;
    internal readonly DropdownField validityField = null;
    internal readonly ListView connectedModulesField = null;
}
}

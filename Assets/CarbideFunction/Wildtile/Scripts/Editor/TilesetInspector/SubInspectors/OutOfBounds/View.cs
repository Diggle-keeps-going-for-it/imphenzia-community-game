using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.OutOfBounds
{
[Serializable]
internal class View
{
    private const string pathToUxml = "TileOutOfBoundsInspector";

    private ListView verticesListView = null;
    internal ListView VerticesListView => verticesListView;

    internal IMGUIContainer userSettingsView = null;
    internal IMGUIContainer UserSettingsView => userSettingsView;

    public int SelectedVertexIndex => verticesListView.selectedIndex;

    public View(VisualElement root)
    {
        var loadedUxml = Resources.Load<VisualTreeAsset>(pathToUxml);
        var uxmlInstance = loadedUxml.CloneTree();
        root.Add(uxmlInstance);
        
        SetSubInspectorRootFlexBehaviour(uxmlInstance);
        CacheVisualElementInstances(uxmlInstance);
        InitialiseUiValues();
    }

    private void SetSubInspectorRootFlexBehaviour(VisualElement root)
    {
        root.style.flexGrow = 1f;
    }

    private void CacheVisualElementInstances(VisualElement root)
    {
        verticesListView = root.Q<ListView>("vertices");
        Assert.IsNotNull(verticesListView);

        userSettingsView = root.Q<IMGUIContainer>("user-settings");
        Assert.IsNotNull(userSettingsView);
 
    }

    private void InitialiseUiValues()
    {
        ListViewBinder.SetupListViewInitial<int>(verticesListView, (uiElement, vertexIndex) => 
        {
            (uiElement as Label).text = vertexIndex.ToString();
        });
    }

    public void UploadCurrentValuesToUi(TransientData transientData)
    {
        ListViewBinder.BindListView(verticesListView, transientData.VertexIndicesOutsideOfBounds);
    }
}
}

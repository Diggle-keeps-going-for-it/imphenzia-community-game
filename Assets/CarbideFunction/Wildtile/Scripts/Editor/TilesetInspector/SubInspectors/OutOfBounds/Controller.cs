using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

using UnityEditor;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.OutOfBounds
{
[Serializable]
internal class Controller
{
    public Controller(View view, TransientData transientData, IInspectorAccess inspectorAccess)
    {
        this.inspectorAccess = inspectorAccess;

        RegisterForUiEvents(view);

        this.view = view;
        this.transientData = transientData;
    }

    private void RegisterForUiEvents(View view)
    {
        view.VerticesListView.onSelectionChange += e => OnVertexSelected();
        view.VerticesListView.RegisterCallback<MouseDownEvent>(OnVerticesMouseDown);
        view.UserSettingsView.onGUIHandler += OnUserSettingsGui;
    }

    private void OnVertexSelected()
    {
        inspectorAccess.RefreshDiagnosticsDrawer();
    }

    private void OnUserSettingsGui()
    {
        using (var changeCheck = new EditorGUI.ChangeCheckScope())
        {
            UserSettings.instance.tilesetInspectorSettings.outOfBoundsVerticesDrawer.OnGui();
            if (changeCheck.changed)
            {
                UserSettings.instance.Save();
            }
        }
    }

    private void OnVerticesMouseDown(MouseDownEvent mouseDownEvent)
    {
        if (mouseDownEvent.clickCount == 2)
        {
            OnVerticesListDoubleClicked();
        }
    }

    private void OnVerticesListDoubleClicked()
    {
        var selectedIndex = view.SelectedVertexIndex;
        if (selectedIndex >= 0 && selectedIndex < view.VerticesListView.itemsSource.Count)
        {
            FrameSceneViewOnVertex();
        }
    }

    private void FrameSceneViewOnVertex()
    {
        var vertexIndexInMesh = transientData.VertexIndicesOutsideOfBounds[view.SelectedVertexIndex];
        var vertexPosition = transientData.CachedVertexPositions[vertexIndexInMesh];
        var surroundingBounds = new Bounds(vertexPosition, Vector3.one * UserSettings.instance.tilesetInspectorSettings.outOfBoundsVerticesDrawer.doubleClickVertexFramingSize);
        inspectorAccess.OpenSceneViewAndFocusOnBounds(surroundingBounds);
    }

    private readonly IInspectorAccess inspectorAccess;
    private readonly View view;
    private readonly TransientData transientData;
}
}

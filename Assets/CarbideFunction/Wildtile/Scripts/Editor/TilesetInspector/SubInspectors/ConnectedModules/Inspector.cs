using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Editor.SubInspectors;
using CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
[Serializable]
internal class Inspector
{
    public Inspector(IInspectorAccess parentInspector, VisualElement rootVisualElement, PersistentData persistentData)
    {
        view = new View(rootVisualElement);
        controller = new Controller(parentInspector, persistentData, view);
    }

    public void PopulateConnectedModulesField()
    {
        controller.PopulateConnectedModulesField();
    }

    public void UploadCurrentValuesToUi()
    {
        controller.UploadCurrentValuesToUi();
    }

    public void PopulateConnectedModulesFieldAndSelectCachedOrFirst()
    {
        controller.PopulateConnectedModulesFieldAndSelectCachedOrFirst();
    }

    public void ClearConnectedModulesField()
    {
        controller.ClearConnectedModulesField();
    }

    internal TilesetInspectorDiagnosticsDrawer.IDiagnosticsDrawer CreateDiagnosticDrawer(TilesetImporterAsset tilesetImporter, CurrentModule currentModule, SubInspectors.PersistentData persistentSubInspectorData)
    {
        return SubInspectors.ConnectedModules.DiagnosticsDrawerCreator.CreateDiagnosticsDrawer
            (
                tilesetImporter,
                currentModule,
                ConnectedModuleInstance,
                persistentSubInspectorData.connectedModules
            );
    }

    internal void PopulateStage()
    {
        controller.PopulateStage();
    }

    private readonly View view;
    private readonly Controller controller;

    private ConnectedModule ConnectedModuleInstance => controller.ConnectedModuleInstance;
}
}

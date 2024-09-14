using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Editor.SubInspectors;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;
using FaceDefinition = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator.ConnectiveFaceDefinition;

namespace CarbideFunction.Wildtile.Editor.SubInspectors.ConnectedModules
{
internal class Controller
{
    public Controller(IInspectorAccess parentInspector, PersistentData persistentData, View view)
    {
        this.parentInspector = parentInspector;
        this.persistentData = persistentData;
        this.view = view;
        UploadStaticChoicesToUi();
        RegisterForEvents();

        invalidConnectedModuleStyle = Resources.Load<StyleSheet>(pathToInvalidConnectedModuleUss);
        Assert.IsNotNull(invalidConnectedModuleStyle);
        invalidMatchingContentsConnectedModuleStyle = Resources.Load<StyleSheet>(pathToInvalidMatchingContentsConnectedModuleUss);
        Assert.IsNotNull(invalidMatchingContentsConnectedModuleStyle);

        ConnectedModulesListView.SetUpListView(view.connectedModulesField, invalidConnectedModuleStyle, invalidMatchingContentsConnectedModuleStyle, () => this.parentInspector.Tileset);
    }

    private void RegisterForEvents()
    {
        view.validityField.RegisterCallback<ChangeEvent<string>>(e => OnValidityChanged());
        view.directionField.RegisterCallback<ChangeEvent<string>>(e => OnDirectionChanged());
        view.connectedModulesField.selectionChanged += e => OnConnectedModuleSelected((ConnectedModule)e.FirstOrDefault());
    }

    public void UploadCurrentValuesToUi()
    {
        view.validityField.index = (int)persistentData.moduleValidity;
        view.directionField.index = (int)persistentData.direction;

        ConnectedModulesListView.SelectConnectedModuleInUi(view.connectedModulesField, parentInspector.Tileset, persistentData.cachedConnectedModule);
    }

    private void OnConnectedModuleSelected(ConnectedModule connectedModule)
    {
        var connectedModuleIdentifier = ConnectedModulesListView.CreateConnectedModuleIdentifier(parentInspector.Tileset, connectedModule);
        if (persistentData.cachedConnectedModule != connectedModuleIdentifier)
        {
            persistentData.cachedConnectedModule = connectedModuleIdentifier;

            if (IsThisInspectorActive())
            {
                SubInspectors.ConnectedModules.DiagnosticsDrawerCreator.TrySendConnectedModuleToStage(parentInspector.TilesetImporterAsset, parentInspector.Stage, persistentData, connectedModule);
                parentInspector.RefreshDiagnosticsDrawer();
            }
        }
    }

    private bool IsThisInspectorActive()
    {
        return true;
    }

    public void PopulateConnectedModulesFieldAndSelectCachedOrFirst()
    {
        PopulateConnectedModulesField();
        ConnectedModulesListView.SelectConnectedModuleInUi(view.connectedModulesField, parentInspector.Tileset, persistentData.cachedConnectedModule);
    }

    public void PopulateConnectedModulesField()
    {
        ConnectedModulesListView.PopulateConnectedModulesField(view.connectedModulesField, parentInspector.CurrentModule.module, parentInspector.Tileset, persistentData.direction, persistentData.moduleValidity);
    }

    private void UploadStaticChoicesToUi()
    {
        view.directionField.choices = Calculator.faceDefinitions.Select(kvp => kvp.name).ToList();
        view.validityField.choices = SubInspectors.ConnectedModules.PersistentData.moduleValidityModeStringToValue.ToList();
    }

    private void OnDirectionChanged()
    {
        var newDirection = (Face)view.directionField.index;
        if (newDirection != persistentData.direction)
        {
            persistentData.direction = newDirection;
            PopulateConnectedModulesFieldAndSelectCachedOrFirst();
        }
    }


    private void OnValidityChanged()
    {
        var newValidity = (SubInspectors.ConnectedModules.PersistentData.ModuleValidity)view.validityField.index;
        if (newValidity != persistentData.moduleValidity)
        {
            persistentData.moduleValidity = newValidity;
            PopulateConnectedModulesFieldAndSelectCachedOrFirst();
        }
    }

    public void ClearConnectedModulesField()
    {
        ConnectedModulesListView.ClearConnectedModulesField(view.connectedModulesField);
    }

    internal void PopulateStage()
    {
        var connectedModule = ConnectedModuleInstance;
        if (connectedModule != null)
        {
            SubInspectors.ConnectedModules.DiagnosticsDrawerCreator.TrySendConnectedModuleToStage(parentInspector.TilesetImporterAsset, parentInspector.Stage, persistentData, connectedModule);
        }
        else
        {
            parentInspector.Stage.ConnectedModuleSelected(Vector3.zero, null, null, parentInspector.TilesetImporterAsset.importerSettings.TileDimensions, 0, false, false);
        }
    }

    internal SubInspectors.ConnectedModules.ConnectedModule ConnectedModuleInstance => (SubInspectors.ConnectedModules.ConnectedModule)view.connectedModulesField.selectedItem;

    private const string pathToInvalidConnectedModuleUss = "InvalidConnectedModule";
    private const string pathToInvalidMatchingContentsConnectedModuleUss = "InvalidMatchingContentsConnectedModule";

    private readonly IInspectorAccess parentInspector;
    private readonly PersistentData persistentData;
    private readonly View view;

    private readonly StyleSheet invalidConnectedModuleStyle = null;
    private readonly StyleSheet invalidMatchingContentsConnectedModuleStyle = null;
}
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor.ImportReport
{

/// <summary>
/// Provides the UI front end of the analysis of user's marching cubes, handling user input and display and delegating all implementation to <see cref="TilesetAnalyzer"/>.
/// </summary>
internal class TilesetReportWindow : EditorWindow
{
    private const string uiTitle = "Tileset Import Report";

    private const string importErrorIntro = "Import failed from an unrecoverable issue: ";

    /// <summary>
    /// Analyze the produced tileset and show the results in the tileset report window.
    /// </summary>
    public static void ShowReport(TilesetImporterAsset importer, ImportDetails importDetails)
    {
        var window = GetWindow<TilesetReportWindow>(title:uiTitle);
        window.Show();
        window.modelImporter = window.permanentModelImporter = importer;
        window.outputTileset = window.permanentOutputTileset = importer.destinationTileset;

        if (!String.IsNullOrEmpty(importDetails.exceptionMessage))
        {
            window.exceptionMessage = importDetails.exceptionMessage;
            window.cachedReport = null;
            window.cachedImportDetails = null;
        }
        else
        {
            window.exceptionMessage = null;
            window.AnalyzeAndShowReport(importDetails);
        }

        window.ShowAndHideMainUiAndImportErrorIfExceptionExists();
    }

    private void AnalyzeAndShowReport(ImportDetails importDetails)
    {
        var searchParameters = TilesetReportWindowParametersAccess.instance.Parameters.missingTileParameters;
        cachedReport = TilesetAnalyzer.GenerateReport(modelImporter.destinationTileset, searchParameters.searchConfigurations);
        cachedImportDetails = importDetails;
        ApplyReportToReportUi(cachedReport, importDetails);
    }

    /// <summary>
    /// Show a custom report. This is intended for use with testing the UI
    /// </summary>
    public static void ShowReportAndAnalysis(TilesetImporterAsset importer, ImportDetails importDetails, TilesetAnalyzer.Report analysisReport)
    {
        var window = GetWindow<TilesetReportWindow>(title:uiTitle);
        window.Show();
        window.modelImporter = window.permanentModelImporter = importer;
        window.outputTileset = window.permanentOutputTileset = importer.destinationTileset;

        if (!String.IsNullOrEmpty(importDetails.exceptionMessage))
        {
            window.exceptionMessage = importDetails.exceptionMessage;
            window.cachedReport = null;
            window.cachedImportDetails = null;
        }
        else
        {
            window.exceptionMessage = null;
            window.cachedReport = analysisReport;
            window.cachedImportDetails = importDetails;
            window.ApplyReportToReportUi(window.cachedReport, window.cachedImportDetails);
        }

        window.ShowAndHideMainUiAndImportErrorIfExceptionExists();
    }

    private VisualElement reportRoot = null;
    private ObjectField modelImporterField = null;
    private ObjectField outputTilesetField = null;
    private VisualElement unconnectableModulesWarning = null;
    private ListView unconnectableModules = null;
    private VisualElement emptyTilesetWarning = null;
    private ListView emptyTileset = null;
    private VisualElement faceOnCubeFaceWarning = null;
    private ListView faceOnCubeFaceModules = null;
    private HelpBox importErrorWarning = null;
    private IMGUIContainer marchingCubePreviewElement = null;

    private ImportReport.SubWindow.MissingTilesSubWindow missingTilesSubWindow = null;
    private ImportReport.SubWindow.HashCollisionSubWindow hashCollisionSubWindow = null;
    private ImportReport.SubWindow.SuperInsideOutsideCornersSubWindow superInsideOutsideModulesSubWindow = null;
    private ImportReport.SubWindow.OutOfBoundsSubWindow outOfBoundsSubWindow = null;

    [SerializeField]
    private TilesetImporterAsset modelImporter = null;
    [SerializeField]
    private TilesetImporterAsset permanentModelImporter = null;
    [SerializeField]
    private Tileset outputTileset = null;
    [SerializeField]
    private Tileset permanentOutputTileset = null;

    [SerializeField]
    private TilesetAnalyzer.Report cachedReport = null;

    [SerializeField]
    private ImportDetails cachedImportDetails = null;

    [SerializeField]
    private string exceptionMessage = null;

    private Mesh missingTilesPreviewMesh = null;

    /// <summary>
    /// Engine-called method to set up the window the first time it is created. Do not call manually. 
    /// </summary>
    public void CreateGUI()
    {
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/CarbideFunction/Wildtile/Scripts/Editor/Resources/TilesetAnalyzer.uxml");
        visualTree.CloneTree(rootVisualElement);

        reportRoot = rootVisualElement.Q<VisualElement>("report");

        modelImporterField = rootVisualElement.Q<ObjectField>("importer-source");
        modelImporterField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnModelImporterChanged);
        outputTilesetField = rootVisualElement.Q<ObjectField>("tileset-output");
        outputTilesetField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(OnOutputTilesetChanged);

        var unconnectableModulesRoot = rootVisualElement.Q<VisualElement>("unconnectable-modules");
        unconnectableModulesWarning = unconnectableModulesRoot.Q<VisualElement>("warning");
        unconnectableModules = unconnectableModulesRoot.Q<ListView>("modules");
        unconnectableModules.RegisterCallback<MouseDownEvent>(e => {if (e.clickCount == 2){OpenUnconnectableModuleInModuleInspector();}});
        var emptyTilesetRoot = rootVisualElement.Q<VisualElement>("empty-marching-cube-buckets");
        emptyTilesetWarning = emptyTilesetRoot.Q<VisualElement>("warning");
        emptyTileset = emptyTilesetRoot.Q<ListView>("marching-cube-layouts");
        var faceOnCubeFaceRoot = rootVisualElement.Q<VisualElement>("face-on-cube-face-modules");
        faceOnCubeFaceWarning = faceOnCubeFaceRoot.Q<VisualElement>("warning");
        faceOnCubeFaceModules = faceOnCubeFaceRoot.Q<ListView>("modules");
        faceOnCubeFaceModules.RegisterCallback<MouseDownEvent>(e => {if (e.clickCount == 2){OpenTrianglesOnModuleFaceInModuleInspector();}});

        emptyTileset.onSelectionChange += OnEmptyTilesetSelectionChanged;

        importErrorWarning = rootVisualElement.Q<HelpBox>("import-error");

        marchingCubePreviewElement = rootVisualElement.Q<IMGUIContainer>("marching-cube-preview");

        var modelImporterGetter = new Func<TilesetImporterAsset>(() => this.permanentModelImporter);

        hashCollisionSubWindow = new SubWindow.HashCollisionSubWindow(rootVisualElement.Q<VisualElement>("colliding-face-hashes"), TilesetReportWindowParametersAccess.instance.Parameters.hashCollisionSubWindowData, modelImporterGetter);
        superInsideOutsideModulesSubWindow = new SubWindow.SuperInsideOutsideCornersSubWindow(rootVisualElement.Q<VisualElement>("super-inside-corners"), modelImporterGetter);
        outOfBoundsSubWindow = new SubWindow.OutOfBoundsSubWindow(rootVisualElement.Q<VisualElement>("out-of-bounds-vertices"), modelImporterGetter);

        BindProperties();
    }

    private void OnEnable()
    {
        // unique flow for missingTilesSubWindow is required because
        // this is done through the IMGUI system. IMGUI is required because
        // only IMGUI allows hot controls, there doesn't seem to be a way to do that in UI Toolkit.
        missingTilesSubWindow = new SubWindow.MissingTilesSubWindow();
        if (marchingCubePreviewElement != null)
        {
            missingTilesSubWindow.Bind(marchingCubePreviewElement);
        }
    }

    private void OnGUI()
    {
        // might be null on a domain reload, before CreateGUI has been called
        if (marchingCubePreviewElement != null)
        {
            var targetBound = marchingCubePreviewElement.worldBound;
            var windowBound = rootVisualElement.worldBound;
            var renderBound = new Rect(targetBound.x - windowBound.x, targetBound.y - windowBound.y, targetBound.width, targetBound.height);
            if (renderBound.width > 0 && renderBound.height > 0)
            {
                var searchParameters = TilesetReportWindowParametersAccess.instance.Parameters.missingTileParameters;
                missingTilesSubWindow.Render(renderBound, searchParameters.renderingData, missingTilesPreviewMesh);
            }
        }
    }

    private void OnDisable()
    {
        missingTilesSubWindow.Destroy();
        missingTilesSubWindow = null;
    }

    private void BindProperties()
    {
        var bindingSource = new SerializedObject(this);
        modelImporterField.BindProperty(bindingSource.FindProperty(nameof(modelImporter)));
        outputTilesetField.BindProperty(bindingSource.FindProperty(nameof(outputTileset)));
        ListViewBinder.SetupListViewInitial<TilesetAnalyzer.UnconnectableModule>(unconnectableModules, (uiElement, unconnectableModule) => (uiElement as Label).text = CreateNameFromUnconnectableModule(unconnectableModule));
        ListViewBinder.SetupListViewInitial<int>(emptyTileset, (uiElement, marchingCubeIndex) => (uiElement as Label).text = CreateNameFromMissingMarchingCubeSearchParameterIndex(marchingCubeIndex));
        ListViewBinder.SetupListViewInitial<ImportDetails.ModuleWithFacesOnACubeFace>(faceOnCubeFaceModules, (uiElement, faceOnBoxFaceModule) => (uiElement as Label).text = CreateNameFromFaceOnBoxFaceModule(faceOnBoxFaceModule));

        if (cachedReport != null && cachedReport.IsValid)
        {
            Assert.IsNotNull(cachedImportDetails);
            ApplyReportToReportUi(cachedReport, cachedImportDetails);
        }

        ShowAndHideMainUiAndImportErrorIfExceptionExists();

        if (missingTilesSubWindow != null)
        {
            missingTilesSubWindow.Bind(marchingCubePreviewElement);
        }
    }

    private static string CreateNameFromUnconnectableModule(TilesetAnalyzer.UnconnectableModule module)
    {
        return module.GetUserFriendlyIdentifier();
    }

    private static string CreateNameFromMissingMarchingCubeSearchParameterIndex(int searchParamIndex)
    {
        var searchParameters = TilesetReportWindowParametersAccess.instance.Parameters.missingTileParameters;
        var searchParam = searchParameters.searchConfigurations[searchParamIndex];
        var marchingCubeIndex = searchParam.marchingCubeConfig;
        return $"{Convert.ToString(marchingCubeIndex, 2).PadLeft(8, '0')} - {Convert.ToString(marchingCubeIndex)}";
    }
    
    private string CreateNameFromFaceOnBoxFaceModule(ImportDetails.ModuleWithFacesOnACubeFace module)
    {
        return $"{module.cachedName} ({module.numberOfFacesOnCubeFace})";
    }
    
    private void ApplyReportToReportUi(TilesetAnalyzer.Report report, ImportDetails importDetails)
    {
        ListViewBinder.BindListView(unconnectableModules, report.unconnectableModules);
        unconnectableModulesWarning.style.display = BoolToDisplayStyle(report.unconnectableModules.GetLength(0) > 0);
        ListViewBinder.BindListView(emptyTileset, report.emptyMarchingCubeBuckets);
        emptyTilesetWarning.style.display = BoolToDisplayStyle(report.emptyMarchingCubeBuckets.GetLength(0) > 0);

        ListViewBinder.BindListView(faceOnCubeFaceModules, importDetails.modulesWithFacesOnACubeFace);
        faceOnCubeFaceWarning.style.display = BoolToDisplayStyle(importDetails.modulesWithFacesOnACubeFace.Count > 0);

        hashCollisionSubWindow.Bind(importDetails.hashCollisions);
        superInsideOutsideModulesSubWindow.Bind(importDetails.superInsideCornerModules);
        outOfBoundsSubWindow.Bind(importDetails.outOfBoundsModules);
    }

    private static DisplayStyle BoolToDisplayStyle(bool isDisplayed)
    {
        return isDisplayed ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OpenUnconnectableModuleInModuleInspector()
    {
        var invalidModuleRecord = (TilesetAnalyzer.UnconnectableModule)unconnectableModules.selectedItem;
        TilesetInspector.ShowWindowAndSelectModuleAndFace(modelImporter, invalidModuleRecord.cachedName, invalidModuleRecord.face);
    }

    private void OpenTrianglesOnModuleFaceInModuleInspector()
    {
        var moduleWithTrisOnBoundsFace = (ImportDetails.ModuleWithFacesOnACubeFace)faceOnCubeFaceModules.selectedItem;
        TilesetInspector.ShowWindowAndSelectModuleAndHighlightTrisOnFace(modelImporter, moduleWithTrisOnBoundsFace.cachedName);
    }

    private void OnModelImporterChanged(ChangeEvent<UnityEngine.Object> changeEvent)
    {
        OnReadOnlyObjectFieldChanged(ref modelImporter, permanentModelImporter);
    }

    private void OnOutputTilesetChanged(ChangeEvent<UnityEngine.Object> changeEvent)
    {
        OnReadOnlyObjectFieldChanged(ref outputTileset, permanentOutputTileset);
    }

    private void OnReadOnlyObjectFieldChanged<AssetType>(ref AssetType field, AssetType targetAsset) where AssetType : UnityEngine.ScriptableObject
    {
        if (field != targetAsset)
        {
            field = targetAsset;
        }
    }

    private void ShowAndHideMainUiAndImportErrorIfExceptionExists()
    {
        if (!String.IsNullOrEmpty(exceptionMessage))
        {
            importErrorWarning.style.display = DisplayStyle.Flex;
            reportRoot.style.display = DisplayStyle.None;

            importErrorWarning.text = importErrorIntro + exceptionMessage;
        }
        else
        {
            importErrorWarning.style.display = DisplayStyle.None;
            reportRoot.style.display = DisplayStyle.Flex;
        }
    }

    private void OnEmptyTilesetSelectionChanged(IEnumerable<System.Object> selectedObjects)
    {
        if (selectedObjects.Count() > 0)
        {
            var selectedConfigIndex = (int)selectedObjects.First();
            var searchParameters = TilesetReportWindowParametersAccess.instance.Parameters.missingTileParameters;
            var selectedObject = searchParameters.searchConfigurations[selectedConfigIndex];

            StartShowingMeshInPreviewWindow(selectedObject?.representativeModel);
        }
        else
        {
            StartShowingMeshInPreviewWindow(null);
        }
    }

    private void StartShowingMeshInPreviewWindow(Mesh mesh)
    {
        missingTilesPreviewMesh = mesh;
    }
}

}

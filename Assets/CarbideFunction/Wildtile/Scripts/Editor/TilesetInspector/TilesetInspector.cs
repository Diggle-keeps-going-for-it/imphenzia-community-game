using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer;

using Calculator = CarbideFunction.Wildtile.Editor.ModelTileConnectivityCalculator;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class manages the UI for the module inspector and interacts with the <see cref="TilesetInspectorStage"/> to show the modules and their connections to the user.
/// </summary>
internal class TilesetInspector : EditorWindow, SubInspectors.IInspectorAccess
{
    private const string pathToUxml = "TilesetInspector";
    private const string uiTitle = "Tileset Inspector";

    [MenuItem("Window/Wildtile/Tileset Inspector")]
    public static void ShowWindow()
    {
        GetWindow();
    }

    private static TilesetInspector GetWindow()
    {
        // Show existing window instance. If one doesn't exist, make one.
        return EditorWindow.GetWindow<TilesetInspector>(title:uiTitle);
    }

    internal static void ShowWindowAndSelectModuleAndFace(TilesetImporterAsset modelImporter, string moduleName, Face targetFace)
    {
        var window = GetWindow();
        window.SetConfig(modelImporter);
        window.SelectCurrentModuleByName(moduleName);
        window.persistentSubInspectorData.connectedModules.direction = targetFace;
        window.persistentSubInspectorData.connectedModules.moduleValidity = SubInspectors.ConnectedModules.PersistentData.ModuleValidity.MatchingVertexContents;
        window.inspectionMode = InspectionMode.Connections;
        window.UploadCurrentValuesToUi();
        window.OpenPreviewStage();
    }

    internal static void ShowWindowAndSelectModuleAndFaceAndOppositeModule(TilesetImporterAsset modelImporter, string moduleName, Face targetFace, string oppositeModuleName, int oppositeModuleYawIndex, bool oppositeModuleIsFlipped)
    {
        var window = GetWindow();
        window.SetConfig(modelImporter);
        window.inspectionMode = InspectionMode.Connections;
        window.persistentSubInspectorData.connectedModules.direction = targetFace;
        window.persistentSubInspectorData.connectedModules.moduleValidity = SubInspectors.ConnectedModules.PersistentData.ModuleValidity.All;
        window.SelectCurrentModuleByName(moduleName);
        window.persistentSubInspectorData.connectedModules.cachedConnectedModule = new SubInspectors.ConnectedModules.CachedConnectedModuleIdentifier{prefabName = oppositeModuleName, yawIndex = oppositeModuleYawIndex, isFlipped = oppositeModuleIsFlipped};
        window.UploadCurrentValuesToUi();
        window.OpenPreviewStage();
    }

    internal static void ShowWindowAndSelectModuleAndHighlightTrisOnFace(TilesetImporterAsset modelImporter, string moduleName)
    {
        var window = GetWindow();
        window.SetConfig(modelImporter);
        window.inspectionMode = InspectionMode.TrianglesOnModuleFace;
        window.SelectCurrentModuleByName(moduleName);
        window.UploadCurrentValuesToUi();
        window.OpenPreviewStage();
    }

    internal static void ShowWindowAndSelectModuleAndCorner(TilesetImporterAsset modelImporter, string moduleName, int cornerIndex)
    {
        var window = GetWindow();
        window.SetConfig(modelImporter);
        window.inspectionMode = InspectionMode.VertexContents;
        window.SelectCurrentModuleByName(moduleName);
        window.vertexIndex = cornerIndex + 1;
        window.UploadCurrentValuesToUi();
        window.OpenPreviewStage();
    }

    internal static void ShowWindowAndSelectModuleAndOutOfBounds(TilesetImporterAsset modelImporter, string moduleName)
    {
        var window = GetWindow();
        window.SetConfig(modelImporter);
        window.inspectionMode = InspectionMode.OutsideOfBounds;
        window.SelectCurrentModuleByName(moduleName);
        window.UploadCurrentValuesToUi();
        window.OpenPreviewStage();
    }

    private ObjectField configField = null;
    private Button reimportButton = null;
    private Button refreshButton = null;
    private ListView modulesField = null;
    private DropdownField inspectionModeField = null;
    private DropdownField vertexIndexField = null;
    private TextField cachedVertexContentsField = null;
    private TextField calculatedVertexContentsField = null;

    private VisualElement connectionControls = null;
    private VisualElement vertexContentsControls = null;
    private VisualElement triOnModuleFaceControls = null;
    private VisualElement outsideBoundsControls = null;

    [Serializable]
    private struct InspectedTilesetImporter
    {
        [SerializeField]
        public TilesetImporterAsset config;

        public List<SelectedModule> cachedModules;

        public InspectedTilesetImporter(TilesetImporterAsset config, List<SelectedModule> modules)
        {
            this.config = config;
            this.cachedModules = modules;
        }
    }

    [SerializeField]
    private InspectedTilesetImporter inspectedImporter = new InspectedTilesetImporter(null, new List<SelectedModule>());
    private Tileset ConfigTileset => inspectedImporter.config?.destinationTileset;

    [SerializeField]
    private CurrentModule currentModule = new CurrentModule(
        String.Empty,
        null
    );

    private IDiagnosticsDrawer diagnosticsDrawer = null;

    private static string ToPositiveNegative(bool isPositive)
    {
        return isPositive ? "+" : "-";
    }
    private readonly static string[] vertexChoices = new []{"Summary"}.Concat(Enumerable.Range(0,8).Select(vertexIndex =>
        {
            var xPositive = ((vertexIndex >> 0) & 1) != 0;
            var yPositive = ((vertexIndex >> 1) & 1) != 0;
            var zPositive = ((vertexIndex >> 2) & 1) != 0;
            return $"{ToPositiveNegative(xPositive)}x {ToPositiveNegative(yPositive)}y {ToPositiveNegative(zPositive)}z";
        }
    )).ToArray();
    [SerializeField]
    private int vertexIndex = 0;

    private enum InspectionMode
    {
        Connections,
        VertexContents,
        TrianglesOnModuleFace,
        OutsideOfBounds,
    }

    // This has to be a string to fit in with the UI bindings.
    // Use the InspectionModeEnum in code to get the value as an enum.
    private readonly static List<string> inspectionModeStrings = new List<string>{
        "Connections",
        "Vertex contents",
        "Triangles on module face",
        "Outside of bounds",
    };
    [SerializeField]
    private InspectionMode inspectionMode = InspectionMode.Connections;

    private SubInspectors.OutOfBounds.Inspector outsideBoundsInspector = null;
    private SubInspectors.ConnectedModules.Inspector connectedModuleInspector = null;

    private void OnEnable()
    {
        var loadedUxml = Resources.Load<VisualTreeAsset>(pathToUxml);
        loadedUxml.CloneTree(rootVisualElement);

        configField = rootVisualElement.Query<ObjectField>("config").First();
        configField.RegisterCallback<ChangeEvent<UnityEngine.Object>>(e => {OnConfigChanged((TilesetImporterAsset)e.newValue);});

        reimportButton = rootVisualElement.Query<Button>("reimport").First();
        reimportButton.clicked += () => {Reimport(inspectedImporter.config);};

        refreshButton = rootVisualElement.Query<Button>("force-refresh").First();
        refreshButton.clicked += () => {RefreshWholeInspector();};

        connectionControls = rootVisualElement.Query<VisualElement>("connections-box").First();
        vertexContentsControls = rootVisualElement.Query<VisualElement>("vertex-contents-box").First();
        triOnModuleFaceControls = rootVisualElement.Query<VisualElement>("tri-on-module-face-box").First();
        outsideBoundsControls = rootVisualElement.Query<VisualElement>("outside-bounds-box").First();

        outsideBoundsInspector = new SubInspectors.OutOfBounds.Inspector(outsideBoundsControls, ThisForSubInspectors);

        modulesField = rootVisualElement.Query<ListView>("modules").First();
        ListViewBinder.SetupListViewInitial<SelectedModule>(modulesField, (uiElement, module) => (uiElement as Label).text = CreateNameFromModule(module.module));
        modulesField.selectionChanged += e => OnModuleSelected(((SelectedModule)e.FirstOrDefault()));
        modulesField.RegisterCallback<MouseDownEvent>(e => {if (e.clickCount == 2){OpenPreviewStage();}});

        inspectionModeField = rootVisualElement.Query<DropdownField>("inspection-mode").First();
        inspectionModeField.RegisterCallback<ChangeEvent<string>>(e => OnInspectionModeChanged());

        connectedModuleInspector = new SubInspectors.ConnectedModules.Inspector(ThisForSubInspectors, rootVisualElement, persistentSubInspectorData.connectedModules);

        vertexIndexField = vertexContentsControls.Query<DropdownField>("vertex").First();
        vertexIndexField.RegisterCallback<ChangeEvent<string>>(e => OnVertexContentsIndexChanged());

        cachedVertexContentsField = vertexContentsControls.Query<TextField>("cached-status").First();
        calculatedVertexContentsField = vertexContentsControls.Query<TextField>("calculated-status").First();

        SetupVertexContentsSettingsImgui(vertexContentsControls);

        // refresh the config instance data, it might be stale/null if after a domain reload
        RefreshInstanceData();

        UploadChoicesToUi();
        UploadCurrentValuesToUi();

        SceneView.duringSceneGui += OnScene;
        TilesetImporterAsset.onTilesetChanged += RefreshModulesIfInspectingInstance;

        EnsureOnlyCurrentInspectionModeControlsShown();

        SetCurrentModulesListViewHeightFromPreferencesAndStartListeningForChanges();
    }

    private void RefreshWholeInspector()
    {
        RefreshInstanceData();
        UploadCurrentValuesToUi();
    }

    private void RefreshInstanceData()
    {
        SetConfig(inspectedImporter.config);
    }

    private void SetupVertexContentsSettingsImgui(VisualElement vertexContentsRoot)
    {
        var settingsPanel = vertexContentsRoot.Q<IMGUIContainer>("settings");
        settingsPanel.onGUIHandler += VertexContentsSettingsOnGui;
    }

    private void VertexContentsSettingsOnGui()
    {
        EditorGUI.BeginChangeCheck();
        UserSettings.instance.tilesetInspectorSettings.vertexContentsDrawer.OnGui();
        if (EditorGUI.EndChangeCheck())
        {
            UserSettings.instance.Save();
        }
    }

    private string CreateNameFromModule(Module module)
    {
        Assert.IsNotNull(module);

        var moduleName = GetModuleNameFromModule(module);

        return $"{moduleName}";
    }

    private string GetModuleNameFromModule(Module module)
    {
        return module.name;
    }

    private string GetModuleNameFromModuleIndex(int moduleIndex)
    {
        if (inspectedImporter.config == null)
        {
            return $"<Module {moduleIndex} - no model importer>";
        }

        return GetModuleNameFromModuleIndex(ConfigTileset, moduleIndex);
    }

    internal static string GetModuleNameFromModuleIndex(Tileset tileset, int moduleIndex)
    {
        if (tileset == null)
        {
            return $"<Module {moduleIndex} - no marching cubes>";
        }

        if (tileset.modules == null)
        {
            return $"<Module {moduleIndex} - no marching cubes.{nameof(Tileset.modules)}>";
        }

        if (tileset.modules.Count <= moduleIndex)
        {
            return $"<Module {moduleIndex} - marching cubes.{nameof(Tileset.modules)} only has {tileset.modules.Count} elements>";
        }

        var module = tileset.modules[moduleIndex];

        if (module == null)
        {
            return $"<Module {moduleIndex} - {nameof(Module)} was null>";
        }

        return module.name;
    }

    private void UploadChoicesToUi()
    {
        inspectionModeField.choices = inspectionModeStrings;
        vertexIndexField.choices = vertexChoices.ToList();
    }

    private void UploadCurrentValuesToUi()
    {
        configField.value = inspectedImporter.config;

        inspectionModeField.index = (int)inspectionMode;
        EnsureOnlyCurrentInspectionModeControlsShown();

        vertexIndexField.index = vertexIndex;

        PopulateUiModuleList();

        SelectCurrentModuleByName(currentModule.name);

        connectedModuleInspector.PopulateConnectedModulesField();

        outsideBoundsInspector.UploadCurrentValuesToUi();
        connectedModuleInspector.UploadCurrentValuesToUi();
    }

    private void SelectCurrentModuleByName(string currentModuleName)
    {
        var modulesSource = inspectedImporter.cachedModules;
        if (modulesSource == null || modulesSource.Count == 0)
        {
            modulesField.selectedIndex = 0;
            modulesField.ScrollToItem(0);
            currentModule = new CurrentModule();
        }
        else
        {
            var foundCurrentModuleIndex = modulesSource.FindIndex(module => CreateNameFromModule(module.module) == currentModuleName);

            if (foundCurrentModuleIndex == -1)
            {
                modulesField.selectedIndex = 0;
                modulesField.ScrollToItem(0);
                var targetModule = modulesSource[0];
                currentModule = new CurrentModule(CreateNameFromModule(targetModule.module), targetModule);
            }
            else
            {
                modulesField.selectedIndex = foundCurrentModuleIndex;
                modulesField.ScrollToItem(foundCurrentModuleIndex);
                currentModule = new CurrentModule(currentModuleName, modulesSource[foundCurrentModuleIndex]);
            }
        }

        PopulateModuleDataUiFromCurrentModule();
    }

    private void SetConfig(TilesetImporterAsset config)
    {
        if (config != null)
        {
            var modules = GetModulesForImporter(config);
            inspectedImporter = new InspectedTilesetImporter(config, modules);
        }
        else
        {
            inspectedImporter = new InspectedTilesetImporter(null, null);
        }
    }

    private List<SelectedModule> GetModulesForImporter(TilesetImporterAsset importer)
    {
        Assert.IsNotNull(importer);

        var tileset = importer.destinationTileset;
        if (tileset != null)
        {
            return GetModulesAndContents(tileset).OrderBy(module => module.module.name).ToList();
        }
        else
        {
            return null;
        }
    }

    private void OnDisable()
    {
        TilesetImporterAsset.onTilesetChanged -= RefreshModulesIfInspectingInstance;
        SceneView.duringSceneGui -= OnScene;
        StopListeningForPreferencesChangesForCurrentModuleListViewHeight();
    }

    private void RefreshModulesIfInspectingInstance(TilesetImporterAsset instance)
    {
        if (System.Object.ReferenceEquals(instance, inspectedImporter.config))
        {
            RefreshInstanceData();
            PopulateUiModuleList();
            UploadCurrentValuesToUi();
        }
    }

    private void OnConfigChanged(TilesetImporterAsset newConfig)
    {
        if (inspectedImporter.config != newConfig)
        {
            SetConfig(newConfig);
            PopulateUiModuleList();
            SelectTopModule();
            RefreshStage();
            RefreshDiagnosticsDrawer();
        }
    }

    private void PopulateUiModuleList()
    {
        var tileset = inspectedImporter.config?.destinationTileset;
        if (tileset != null)
        {
            ListViewBinder.BindListView(modulesField, inspectedImporter.cachedModules);
            reimportButton.SetEnabled(true);
        }
        else
        {
            ListViewBinder.BindListView(modulesField, new List<SelectedModule>());
            connectedModuleInspector.ClearConnectedModulesField();
            reimportButton.SetEnabled(false);
        }
    }

    private void SelectTopModule()
    {
        // force event to fire by changing off and on again
        modulesField.selectedIndex = -1;
        modulesField.selectedIndex = 0;
    }

    private static IEnumerable<SelectedModule> GetModulesAndContents(Tileset cubes)
    {
        foreach (var cubeConfigIndex in Enumerable.Range(0, 256))
        {
            foreach (var transformedModule in cubes.MarchingCubeLookup[cubeConfigIndex].availableModules)
            {
                if (transformedModule.yawIndex == 0 && !transformedModule.isFlipped)
                {
                    yield return new SelectedModule{
                        module = cubes.modules[transformedModule.moduleIndex],
                        moduleIndex = transformedModule.moduleIndex,
                        contents = cubeConfigIndex
                    };
                }
            }
        }
    }

    private void OnModuleSelected(SelectedModule selectedModule)
    {
        if (currentModule.module != selectedModule)
        {
            CacheSelectedModuleAsCurrent(selectedModule);
            PopulateModuleDataUiFromCurrentModule();
        }
    }


    private void CacheSelectedModuleAsCurrent(SelectedModule selectedModule)
    {
        currentModule = new CurrentModule(SafeCreateNameFromModule(selectedModule), selectedModule);
    }

    private string SafeCreateNameFromModule(SelectedModule selectedModule)
    {
        if (selectedModule != null)
        {
            return CreateNameFromModule(selectedModule.module);
        }
        else
        {
            return String.Empty;
        }
    }

    private void PopulateModuleDataUiFromCurrentModule()
    {
        if (currentModule.module == null)
        {
            connectedModuleInspector.ClearConnectedModulesField();
            ClearVertexContentsTextBoxes();
            ClearOutOfBoundsInspector();
        }
        else
        {
            connectedModuleInspector.PopulateConnectedModulesFieldAndSelectCachedOrFirst();
            PopulateVertexContentsTextBoxes();
            PopulateOutOfBoundsInspector();
        }

        UploadCurrentModuleToMaybeStage();

        RefreshDiagnosticsDrawer();
    }

    [SerializeField]
    private TilesetInspectorStage stage = null;

    private void EnsureStageInstantiated()
    {
        if (stage == null)
        {
            stage = ScriptableObject.CreateInstance<TilesetInspectorStage>();
        }
    }

    private void OpenPreviewStage()
    {
        EnsureStageInstantiated();
        StageUtility.GoToStage(stage, false);

        if (currentModule.module != null)
        {
            if (inspectedImporter.config != null)
            {
                UploadCurrentModuleToStage();
            }
        }

        if (inspectionMode == InspectionMode.Connections)
        {
            connectedModuleInspector.PopulateStage();
        }
    }

    private Module GetModuleForTransformedModule(TransformedModule transformedModule)
    {
        if (transformedModule == null)
        {
            throw new ArgumentNullException();
        }

        if (ConfigTileset == null)
        {
            throw new NullReferenceException();
        }

        if (ConfigTileset.modules == null)
        {
            throw new NullReferenceException();
        }

        if (ConfigTileset.modules.Count() <= transformedModule.moduleIndex)
        {
            throw new IndexOutOfRangeException();
        }

        return ConfigTileset.modules[transformedModule.moduleIndex];
    }

    private void RefreshStage()
    {
        if (stage != null)
        {
            if (currentModule.module != null)
            {
                UploadCurrentModuleToStage();
            }

            if (inspectionMode == InspectionMode.Connections)
            {
                connectedModuleInspector.PopulateStage();
            }
            else
            {
                stage.ConnectedModuleSelected(Vector3.zero, null, null, inspectedImporter.config.importerSettings.TileDimensions, 0, false, false);
            }
        }
    }

    private void UploadCurrentModuleToStage()
    {
        var module = currentModule.module?.module;
        var prefab = module?.prefab;
        var mesh = module?.mesh;

        stage.ModuleSelected(prefab, mesh, inspectedImporter.config.importerSettings.TileDimensions, 0, false);
    }

    private void UploadCurrentModuleToMaybeStage()
    {
        var module = currentModule.module?.module;
        var prefab = module?.prefab;
        var mesh = module?.mesh;

        stage?.ModuleSelected(prefab, mesh, inspectedImporter.config.importerSettings.TileDimensions, 0, false);
    }

    private void OnInspectionModeChanged()
    {
        var newInspectionMode = (InspectionMode)inspectionModeField.index;
        if (inspectionMode != newInspectionMode)
        {
            inspectionMode = newInspectionMode;
            EnsureOnlyCurrentInspectionModeControlsShown();
        }
    }

    private void EnsureOnlyCurrentInspectionModeControlsShown()
    {
        HideAllInspectionModeGroups();
        ShowSelectedInspectionModeGroup();

        RefreshStage();
        RefreshDiagnosticsDrawer();
    }

    private void HideAllInspectionModeGroups()
    {
        connectionControls.style.display = DisplayStyle.None;
        vertexContentsControls.style.display = DisplayStyle.None;
        triOnModuleFaceControls.style.display = DisplayStyle.None;
        outsideBoundsControls.style.display = DisplayStyle.None;
    }

    private void ShowSelectedInspectionModeGroup()
    {
        switch (inspectionMode)
        {
            case InspectionMode.Connections:
            {
                connectionControls.style.display = DisplayStyle.Flex;
                break;
            }
            case InspectionMode.VertexContents:
            {
                vertexContentsControls.style.display = DisplayStyle.Flex;
                break;
            }
            case InspectionMode.TrianglesOnModuleFace:
            {
                triOnModuleFaceControls.style.display = DisplayStyle.Flex;
                break;
            }
            case InspectionMode.OutsideOfBounds:
            {
                outsideBoundsControls.style.display = DisplayStyle.Flex;
                break;
            }
            default:
            {
                Debug.LogError($"Attempting to show inspection mode group for unknown inspection mode: {inspectionMode}");
                break;
            }
        }
    }

    private void OnVertexContentsIndexChanged()
    {
        var newVertexContentsIndex = vertexIndexField.index;
        if (vertexIndex != newVertexContentsIndex)
        {
            vertexIndex = newVertexContentsIndex;

            if (currentModule.module != null)
            {
                PopulateVertexContentsTextBoxes();
            }
            else
            {
                ClearVertexContentsTextBoxes();
            }
            RefreshDiagnosticsDrawer();
        }
    }

    private class InsidenessReporter : Calculator.VertexContentsInferenceDiagnostics
    {
        public int reportingVertexIndex;
        public delegate void ReportVertexInsideness(int insideness);
        public ReportVertexInsideness reportVertexInsideness;

        private bool IsCorrectVertexIndex(int vertexIndex)
        {
            return reportingVertexIndex == vertexIndex;
        }

        public override void OnFirstTriSelected(int vertexIndex, Triangle tri, bool isClockwise)
        {
        }

        public override void OnTriMissed(int vertexIndex, Triangle tri)
        {
        }

        public override void OnTriCrossed(int vertexIndex, Triangle tri, bool isClockwise)
        {
        }

        public override void OnNoValidTrianglesForVertex(int vertexIndex)
        {
        }

        public override void OnCubeCornerEvaluated(int vertexIndex, int insideness)
        {
            if (IsCorrectVertexIndex(vertexIndex))
            {
                reportVertexInsideness?.Invoke(insideness);
            }
        }
    }

    private string InsidenessToString(int insideness)
    {
        return insideness > 0 ? "filled" : "empty";
    }

    private void PopulateVertexContentsTextBoxes()
    {
        if (vertexIndex == 0)
        {
            Assert.IsNotNull(currentModule.module);
            var module = currentModule.module.module;
            Assert.IsNotNull(inspectedImporter.config);
            Assert.IsNotNull(inspectedImporter.config.importerSettings);
            Assert.IsNotNull(module);
            var connectivity = Calculator.GetConnectivityFromMesh(module.mesh, inspectedImporter.config.importerSettings.materialImportSettings, module.name);

            var reversedCalculatedContents = connectivity.moduleVertexContents.Reverse();
            calculatedVertexContentsField.value = $"{String.Join("", reversedCalculatedContents.Take(4)) + "-" + String.Join("", reversedCalculatedContents.Skip(4).Take(4))}";
            var cachedContentsConcatenated = Convert.ToString(currentModule.module.contents, 2).PadLeft(8, '0');
            cachedVertexContentsField.value = cachedContentsConcatenated.Substring(0, 4) + "-" + cachedContentsConcatenated.Substring(4, 4);
        }
        else
        {
            var inspectedVertexIndex = vertexIndex - 1;
            // ensure the insideness is set, if the connectivity calculator fails it won't populate the insideness
            calculatedVertexContentsField.value = InsidenessToString(0);
            var diagnostics = new InsidenessReporter{reportingVertexIndex = vertexIndex, reportVertexInsideness = insideness => calculatedVertexContentsField.value = InsidenessToString(insideness)};
            Assert.IsNotNull(currentModule.module);
            var module = currentModule.module.module;
            Assert.IsNotNull(inspectedImporter.config);
            Assert.IsNotNull(inspectedImporter.config.importerSettings);
            Assert.IsNotNull(module);
            Calculator.GetConnectivityFromMesh(module.mesh, inspectedImporter.config.importerSettings.materialImportSettings, module.name, diagnostics);

            var cachedInsideness = (currentModule.module.contents & (1 << vertexIndex)) != 0 ? 1 : 0;
            cachedVertexContentsField.value = InsidenessToString(cachedInsideness);
        }
    }

    private const string emptyVertexContentsString = "-";
    private void ClearVertexContentsTextBoxes()
    {
        calculatedVertexContentsField.value = emptyVertexContentsString;
        cachedVertexContentsField.value = emptyVertexContentsString;
    }

    private void PopulateOutOfBoundsInspector()
    {
        var module = currentModule.module.module;
        Assert.IsNotNull(inspectedImporter.config);
        Assert.IsNotNull(inspectedImporter.config.importerSettings);
        Assert.IsNotNull(module);
        var connectivity = Calculator.GetConnectivityFromMesh(module.mesh, inspectedImporter.config.importerSettings.materialImportSettings, module.name);
        
        var mesh = currentModule.module.module.mesh;
        outsideBoundsInspector.UpdateVerticesFromConnectivity(connectivity, mesh);
        outsideBoundsInspector.UploadCurrentValuesToUi();
    }

    private void ClearOutOfBoundsInspector()
    {
        outsideBoundsInspector.Clear();
    }

    private void OnScene(SceneView view)
    {
        if (StageUtility.GetCurrentStage() != stage)
        {
            return;
        }

        if (currentModule.module != null)
        {
            diagnosticsDrawer?.Draw(view);
        }

        // stopgap to clean up state
        Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
    }

    private void RefreshDiagnosticsDrawer()
    {
        EnsureDiagnosticsDrawerIsDestroyed();
        switch (inspectionMode)
        {
            case InspectionMode.Connections:
            {
                diagnosticsDrawer = connectedModuleInspector.CreateDiagnosticDrawer(inspectedImporter.config, currentModule, persistentSubInspectorData);
                break;
            }
            case InspectionMode.VertexContents:
            {
                diagnosticsDrawer = CreateVertexContentsDiagnosticsDrawer();
                break;
            }
            case InspectionMode.TrianglesOnModuleFace:
            {
                diagnosticsDrawer = CreateTrisOnModuleFaceDiagnosticsDrawer();
                break;
            }
            case InspectionMode.OutsideOfBounds:
            {
                diagnosticsDrawer = CreateOutOfBoundsDiagnosticsDrawer();
                break;
            }
        }
        RedrawSceneViews.Redraw();
    }

    private void EnsureDiagnosticsDrawerIsDestroyed()
    {
        if (diagnosticsDrawer != null)
        {
            diagnosticsDrawer.Shutdown();
        }

        diagnosticsDrawer = null;
    }

    private class VertexContentsInferenceDiagnostics : Calculator.VertexContentsInferenceDiagnostics
    {
        public Vector3 tileDimensions = Vector3.one;
        public int diagnosingVertexIndex;

        public Vector3 triCastStartPoint = Vector3.zero;
        public Vector3 cubeCornerPosition = Vector3.zero;

        public List<Triangle> outwardTriangles = new List<Triangle>();
        public List<Triangle> inwardTriangles = new List<Triangle>();
        public int insideness = 0;

        public override void OnFirstTriSelected(int vertexIndex, Triangle tri, bool isClockwise)
        {
            if (ShouldDrawForVertex(vertexIndex))
            {
                AddTriangleToRelevantList(tri, !isClockwise);
                triCastStartPoint = tri.Aggregate(Vector3.zero, (a,b) => a+b) / 3;
            }
        }

        private void AddTriangleToRelevantList(Triangle tri, bool isInside)
        {
            if (isInside)
            {
                outwardTriangles.Add(tri);
            }
            else
            {
                inwardTriangles.Add(tri);
            }
        }

        private bool ShouldDrawForVertex(int vertexIndex)
        {
            return vertexIndex == diagnosingVertexIndex;
        }

        public override void OnTriMissed(int vertexIndex, Triangle tri)
        {
        }

        public override void OnTriCrossed(int vertexIndex, Triangle tri, bool isClockwise)
        {
            if (ShouldDrawForVertex(vertexIndex))
            {
                AddTriangleToRelevantList(tri, isClockwise);
            }
        }

        public override void OnNoValidTrianglesForVertex(int vertexIndex)
        {
            Reset();
        }

        public override void OnCubeCornerEvaluated(int vertexIndex, int insideness)
        {
            if (ShouldDrawForVertex(vertexIndex))
            {
                cubeCornerPosition = Vector3.Scale(new Vector3(vertexIndex % 2, (vertexIndex / 2) % 2, (vertexIndex / 4) % 2) - Vector3.one * 0.5f, tileDimensions);
                this.insideness = insideness;

            }
            Reset();
        }

        private void Reset()
        {
        }
    }

    private IDiagnosticsDrawer CreateVertexContentsDiagnosticsDrawer()
    {
        if (currentModule.module?.module != null)
        {
            if (vertexIndex == 0)
            {
                var module = currentModule.module.module;
                var connectivity = Calculator.GetConnectivityFromMesh(module.mesh, inspectedImporter.config.importerSettings.materialImportSettings, module.name);

                return new VertexContentsSummaryDrawer(
                    connectivity.moduleVertexContents,
                    new Vector3(inspectedImporter.config.importerSettings.tileWidth, inspectedImporter.config.importerSettings.tileHeight, inspectedImporter.config.importerSettings.tileWidth),
                    UserSettings.instance.tilesetInspectorSettings.vertexContentsSummaryDrawer
                );
            }
            else
            {
                var actualVertexIndex = vertexIndex - 1;
                var diagnostics = new VertexContentsInferenceDiagnostics{tileDimensions = inspectedImporter.config.importerSettings.TileDimensions, diagnosingVertexIndex = actualVertexIndex};
                // run through the contents inference system
                var module = currentModule.module.module;
                Calculator.GetConnectivityFromMesh(module.mesh, inspectedImporter.config.importerSettings.materialImportSettings, module.name, diagnostics);

                if (diagnostics.triCastStartPoint != diagnostics.cubeCornerPosition)
                {
                    return new VertexContentsDrawer(
                        CreateOrderedListOfHitTriangles(
                            diagnostics.triCastStartPoint, diagnostics.cubeCornerPosition,
                            diagnostics.inwardTriangles, diagnostics.outwardTriangles
                        ),
                        diagnostics.cubeCornerPosition,
                        UserSettings.instance.tilesetInspectorSettings.vertexContentsDrawer
                    );
                }
            }
        }

        return null;
    }

    private static List<VertexContentsDrawer.CrossedTriangle> CreateOrderedListOfHitTriangles
    (
        Vector3 startPoint,
        Vector3 endPoint,
        List<Triangle> outwardTriangles,
        List<Triangle> inwardTriangles
    )
    {
        return outwardTriangles.Select(outTri => CreateVertexContentsDrawerTri(startPoint, endPoint, outTri, isHittingFrontFace:true))
            .Concat(inwardTriangles.Select(inTri => CreateVertexContentsDrawerTri(startPoint, endPoint, inTri, isHittingFrontFace:false)))
            .OrderBy(tri => (tri.hitWorldPosition - startPoint).sqrMagnitude)
            .ToList();
    }

    private static VertexContentsDrawer.CrossedTriangle CreateVertexContentsDrawerTri
    (
        Vector3 startPoint,
        Vector3 endPoint,
        Triangle triangle,
        bool isHittingFrontFace
    )
    {
        return new VertexContentsDrawer.CrossedTriangle{
            triangle = triangle,
            hitWorldPosition = CalculateTriangleLineIntersection(startPoint, endPoint, triangle),
            isHittingFrontFace = isHittingFrontFace
        };
    }

    private static Vector3 CalculateTriangleLineIntersection(Vector3 startPoint, Vector3 endPoint, Triangle triangle)
    {
        var trianglePlane = new Plane(Vector3.Cross(triangle.vertex1 - triangle.vertex0, triangle.vertex2 - triangle.vertex0), triangle.vertex0);
        var lineRay = new Ray(startPoint, endPoint - startPoint);

        trianglePlane.Raycast(lineRay, out var distanceAlongRay);

        return lineRay.GetPoint(distanceAlongRay);
    }

    private IDiagnosticsDrawer CreateTrisOnModuleFaceDiagnosticsDrawer()
    {
        if (currentModule.module?.module != null)
        {
            Assert.IsNotNull(inspectedImporter.config);
            Assert.IsNotNull(inspectedImporter.config.importerSettings);
            Assert.IsNotNull(currentModule.module);
            Assert.IsNotNull(currentModule.module.module);

            var mesh = currentModule.module.module.mesh;

            // meshComponent can be missing if the user has selected the filled or empty modules
            if (mesh != null)
            {
                var connectivity = Calculator.GetConnectivityFromMesh(mesh, inspectedImporter.config.importerSettings.materialImportSettings, currentModule.module.module.name);
                Assert.IsNotNull(connectivity);
                return new TrianglesOnModuleFaceDrawer(
                    TrianglesOnModuleFaceMeshCreator.CreateMesh(connectivity.trianglesOnModuleBoundsFace, mesh),
                    inspectedImporter.config.importerSettings.TileDimensions,
                    rootVisualElement
                );
            }
        }

        return null;
    }

    private IDiagnosticsDrawer CreateOutOfBoundsDiagnosticsDrawer()
    {
        var mesh = currentModule.module?.module?.mesh;
        if (mesh != null)
        {
            var connectivity = Calculator.GetConnectivityFromMesh(mesh, inspectedImporter.config.importerSettings.materialImportSettings, currentModule.module.module.name);

            Assert.IsNotNull(connectivity);
            return new OutOfBoundsDrawer(
                mesh?.vertices ?? new Vector3[]{},
                connectivity.vertexIndicesOutsideOfBounds,
                outsideBoundsInspector.SelectedVertexIndex,
                inspectedImporter.config,
                UserSettings.instance.tilesetInspectorSettings.outOfBoundsVerticesDrawer
            );
        }

        return null;
    }

    private void Reimport(TilesetImporterAsset importer)
    {
        if (importer.destinationTileset == null)
        {
            var missingOutputTilesetTitle = "Output Tileset missing";
            var missingOutputTilesetMessage = $"The selected Importer ({importer.name}) is missing its output Tileset, so nothing will be saved.\n\nCreate a Tileset asset by context clicking in the project window and selecting \"Create\" -> \"Wildtile\" -> \"Tileset\", then select the importer and drag the new tileset to the \"Destination Tileset\" property.";
            EditorUtility.DisplayDialog(missingOutputTilesetTitle, missingOutputTilesetMessage, "OK");

            reimportButton.SetEnabled(false);
        }
        else
        {
            TilesetImporter.Import(importer);
        }
    }

    private void SetCurrentModulesListViewHeightFromPreferencesAndStartListeningForChanges()
    {
        SetListViewHeightFromPreferences();
        UserSettings.instance.tilesetInspectorSettings.onCurrentModulesListViewHeightChanged += OnPreferencesChangedForCurrentModuleListViewHeight;
    }

    private void OnPreferencesChangedForCurrentModuleListViewHeight()
    {
        SetListViewHeightFromPreferences();
    }

    private void SetListViewHeightFromPreferences()
    {
        // When the list view is empty (e.g. no tileset importer is selected) Unity adds 100 to the height before doing layout.
        // The maxHeight is not affected and holds this exact value.
        modulesField.style.maxHeight = modulesField.style.height = UserSettings.instance.tilesetInspectorSettings.currentModulesListViewHeight;
    }

    private void StopListeningForPreferencesChangesForCurrentModuleListViewHeight()
    {
        UserSettings.instance.tilesetInspectorSettings.onCurrentModulesListViewHeightChanged -= OnPreferencesChangedForCurrentModuleListViewHeight;
    }

    private void OpenSceneViewAndFocusOnBounds(Bounds frameTarget)
    {
        var instantlyMoveToFocusPosition = false;

        if (StageUtility.GetCurrentStage() != stage)
        {
            OpenPreviewStage();
            instantlyMoveToFocusPosition = true;
        }

        foreach (SceneView sceneView in SceneView.sceneViews)
        {
            sceneView.Frame(frameTarget, instant : instantlyMoveToFocusPosition);
        }
    }

    [SerializeField]
    private SubInspectors.PersistentData persistentSubInspectorData = new SubInspectors.PersistentData();

    void SubInspectors.IInspectorAccess.RefreshDiagnosticsDrawer()
    {
        RefreshDiagnosticsDrawer();
    }

    void SubInspectors.IInspectorAccess.OpenSceneViewAndFocusOnBounds(Bounds frameBounds)
    {
        OpenSceneViewAndFocusOnBounds(frameBounds);
    }

    TilesetImporterAsset SubInspectors.IInspectorAccess.TilesetImporterAsset => inspectedImporter.config;
    Tileset SubInspectors.IInspectorAccess.Tileset => inspectedImporter.config?.destinationTileset;
    TilesetInspectorStage SubInspectors.IInspectorAccess.Stage => stage;
    CurrentModule SubInspectors.IInspectorAccess.CurrentModule => currentModule;

    private SubInspectors.IInspectorAccess ThisForSubInspectors => (SubInspectors.IInspectorAccess)this;
}

}

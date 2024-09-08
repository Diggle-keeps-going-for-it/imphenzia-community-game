using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.ImportReport.SubWindow
{

/// <summary>
/// Manager and renderer for the modules with out-of-bounds vertices sub-window inside the <see cref="TilesetReportWindow"/>.
/// </summary>
internal class OutOfBoundsSubWindow
{
    private HelpBox warningBox = null;
    private ListView listView = null;
    private Func<TilesetImporterAsset> importerGetter = null;

    /// <summary>
    /// Sets up the sub window for rendering immediately
    /// </summary>
    /// <param name="subWindowRoot">The UI element that this subwindow will render into.</param>
    public OutOfBoundsSubWindow(VisualElement subWindowRoot, Func<TilesetImporterAsset> importerGetter)
    {
        this.importerGetter = importerGetter;
        warningBox = subWindowRoot.Q<HelpBox>("warning");
        listView = subWindowRoot.Q<ListView>("modules-with-out-of-bounds-vertices");
        InitializeListBox(listView);
        RegisterForListViewDoubleClick(listView);
    }

    /// <summary>
    /// When the out of bounds modules change, call this to update the window to match the new modules.
    /// </summary>
    public void Bind(List<ImportDetails.OutOfBoundsModule> outOfBoundsModuleNames)
    {
        ShowOrHideWarningBasedOnOutOfBoundsModules(outOfBoundsModuleNames);
        BindListBoxData(outOfBoundsModuleNames);
    }

    private void InitializeListBox(ListView listView)
    {
        ListViewBinder.SetupListViewInitial<ImportDetails.OutOfBoundsModule>(listView, ApplyModuleNameToUi);
    }

    private void ShowOrHideWarningBasedOnOutOfBoundsModules(List<ImportDetails.OutOfBoundsModule> outOfBoundsModules)
    {
        warningBox.style.display = outOfBoundsModules.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindListBoxData(List<ImportDetails.OutOfBoundsModule> outOfBoundsModuleNames)
    {
        ListViewBinder.BindListView(listView, outOfBoundsModuleNames);
    }

    private void ApplyModuleNameToUi(VisualElement element, ImportDetails.OutOfBoundsModule outOfBoundsModule)
    {
        var label = (Label)element;
        label.text = outOfBoundsModule.cachedName;
    }

    private void RegisterForListViewDoubleClick(ListView listView)
    {
        listView.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
    }

    private void OnListViewMouseDown(MouseDownEvent mouseDown)
    {
        if (mouseDown.clickCount == 2)
        {
            var targetItem = (ImportDetails.OutOfBoundsModule)listView.selectedItem;
            if (targetItem != null)
            {
                OpenOutOfBoundsModuleInInspector(targetItem.cachedName);
            }
        }
    }

    private void OpenOutOfBoundsModuleInInspector(string outOfBoundsModuleName)
    {
        TilesetInspector.ShowWindowAndSelectModuleAndOutOfBounds(importerGetter(), outOfBoundsModuleName);
    }
}

}

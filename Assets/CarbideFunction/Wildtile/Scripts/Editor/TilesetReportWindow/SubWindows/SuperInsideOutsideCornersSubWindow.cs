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
/// Manager and renderer for the super inside/outside corners sub-window inside the <see cref="TilesetReportWindow"/>.
/// </summary>
internal class SuperInsideOutsideCornersSubWindow
{
    private HelpBox warningBox = null;
    private ListView listView = null;
    private Func<TilesetImporterAsset> importerGetter = null;

    /// <summary>
    /// Sets up the sub window for rendering immediately
    /// </summary>
    /// <param name="subWindowRoot">The UI element that this subwindow will render into.</param>
    public SuperInsideOutsideCornersSubWindow(VisualElement subWindowRoot, Func<TilesetImporterAsset> importerGetter)
    {
        this.importerGetter = importerGetter;
        warningBox = subWindowRoot.Q<HelpBox>("warning");
        listView = subWindowRoot.Q<ListView>("super-corners");
        InitializeListBox(listView);
        RegisterForListViewDoubleClick(listView);
    }

    /// <summary>
    /// When the super modules change, call this to update the window to match the new collisions.
    /// </summary>
    public void Bind(List<ImportDetails.SuperInsideCornerModule> superModules)
    {
        ShowOrHideWarningBasedOnSuperInsideCornerModules(superModules);
        BindListBoxData(superModules);
    }

    private void InitializeListBox(ListView listView)
    {
        ListViewBinder.SetupListViewInitial<ImportDetails.SuperInsideCornerModule>(listView, ApplySuperInsideCornerModuleToUi);
    }

    private void ShowOrHideWarningBasedOnSuperInsideCornerModules(List<ImportDetails.SuperInsideCornerModule> superModules)
    {
        warningBox.style.display = superModules.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindListBoxData(List<ImportDetails.SuperInsideCornerModule> superModules)
    {
        ListViewBinder.BindListView(listView, superModules);
    }

    private void ApplySuperInsideCornerModuleToUi(VisualElement element, ImportDetails.SuperInsideCornerModule superModule)
    {
        var label = (Label)element;
        label.text = superModule.cachedName;
    }

    private void RegisterForListViewDoubleClick(ListView listView)
    {
        listView.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
    }

    private void OnListViewMouseDown(MouseDownEvent mouseDown)
    {
        if (mouseDown.clickCount == 2)
        {
            var targetItem = (ImportDetails.SuperInsideCornerModule)listView.selectedItem;
            if (targetItem != null)
            {
                OpenCollidingHashInInspector(targetItem);
            }
        }
    }

    private void OpenCollidingHashInInspector(ImportDetails.SuperInsideCornerModule superInsideModule)
    {
        TilesetInspector.ShowWindowAndSelectModuleAndCorner(importerGetter(), superInsideModule.cachedName, superInsideModule.firstSuperInsideOutsideCornerIndex);
    }
}

}

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
/// Manager and renderer for the hash collision sub-window inside the <see cref="TilesetReportWindow"/>.
/// </summary>
internal class HashCollisionSubWindow
{
    [Serializable]
    public class Data
    {
        public VisualTreeAsset listItemTemplate;
        public int listItemHeight;
    }

    private HelpBox warningBox = null;
    private ListView listView = null;
    private Func<TilesetImporterAsset> importerGetter = null;

    /// <summary>
    /// Sets up the sub window for rendering immediately
    /// </summary>
    /// <param name="subWindowRoot">The UI element that this subwindow will render into.</param>
    public HashCollisionSubWindow(VisualElement subWindowRoot, Data data, Func<TilesetImporterAsset> importerGetter)
    {
        this.importerGetter = importerGetter;
        warningBox = subWindowRoot.Q<HelpBox>("warning");
        listView = subWindowRoot.Q<ListView>("collisions");
        InitializeListBox(listView, data);
        RegisterForListViewDoubleClick(listView);
    }

    /// <summary>
    /// When the hash collisions change, call this to update the window to match the new collisions.
    /// </summary>
    public void Bind(List<ImportDetails.HashCollision> hashCollisions)
    {
        ShowOrHideWarningBasedOnHashCollisions(hashCollisions);
        BindListBoxData(hashCollisions);
    }

    private void InitializeListBox(ListView listView, Data data)
    {
        ListViewBinder.SetupListViewInitial<ImportDetails.HashCollision>(listView, ApplyHashCollisionToUi, data.listItemHeight, () => data.listItemTemplate.CloneTree());
    }

    private void ShowOrHideWarningBasedOnHashCollisions(List<ImportDetails.HashCollision> hashCollisions)
    {
        warningBox.style.display = hashCollisions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void BindListBoxData(List<ImportDetails.HashCollision> hashCollisions)
    {
        ListViewBinder.BindListView(listView, hashCollisions);
    }

    private void ApplyHashCollisionToUi(VisualElement element, ImportDetails.HashCollision collisionData)
    {
        var label = element.Q<Label>("label");
        label.text = $"{ModuleFaceToString(collisionData.moduleFace0)} - {OppositeModuleFaceToString(collisionData.moduleFace1)}";
            
    }

    private string ModuleFaceToString(ModuleFaceIdentifier faceIdentifier)
    {
        return $"{faceIdentifier.module} yaw {faceIdentifier.yawIndex * 90}{(faceIdentifier.flipped ? " (flipped)" : "")} {faceIdentifier.face}";
    }

    private string OppositeModuleFaceToString(ModuleFaceIdentifier faceIdentifier)
    {
        return $"{faceIdentifier.module} yaw {faceIdentifier.yawIndex * 90}{(faceIdentifier.flipped ? " (flipped)" : "")}";
    }


    private void RegisterForListViewDoubleClick(ListView listView)
    {
        listView.RegisterCallback<MouseDownEvent>(OnListViewMouseDown);
    }

    private void OnListViewMouseDown(MouseDownEvent mouseDown)
    {
        if (mouseDown.clickCount == 2)
        {
            var targetItem = (ImportDetails.HashCollision)listView.selectedItem;
            if (targetItem != null)
            {
                OpenCollidingHashInInspector(targetItem);
            }
        }
    }

    private void OpenCollidingHashInInspector(ImportDetails.HashCollision collisionItem)
    {
        TilesetInspector.ShowWindowAndSelectModuleAndFaceAndOppositeModule(importerGetter(), collisionItem.moduleFace0.module, collisionItem.moduleFace0.face, collisionItem.moduleFace1.module, collisionItem.moduleFace1.yawIndex, collisionItem.moduleFace1.flipped);
    }
}

}

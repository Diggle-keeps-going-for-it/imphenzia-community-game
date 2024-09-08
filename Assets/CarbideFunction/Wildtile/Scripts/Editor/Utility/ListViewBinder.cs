using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class contains common functions for binding a ListView to a list in Unity's UI Toolkit.
/// </summary>
public static class ListViewBinder
{
    /// <summary>
    /// Delegate that should take <paramref name="contents"/> and apply them somehow to <paramref name="element"/>.
    ///
    /// Depending on TContents and the spawned VisualElement you may want to simply copy a name to a label, or populate images with textures from the <paramref name="contents"/>.
    /// </summary>
    public delegate void ApplyContentsToUi<TContents>(VisualElement element, TContents contents);

    public const int defaultItemHeight = 16;

    /// <summary>
    /// Set up a list view for the first time. This only needs to be called once for a list view, and must be called before <see cref="BindListView"/> is called.
    /// </summary>
    /// <param name="createEmptyItemElement">Pass in a custom constructor that will be used to create different UI elements in the list view. If this parameter is missing or <c>null</c> then <see cref="ListViewBinder"/> will default to creating a single label for each item.</param>
    public static void SetupListViewInitial<TContents>(ListView listView, ApplyContentsToUi<TContents> applyContentsToUi, int itemHeight = defaultItemHeight, Func<VisualElement> createEmptyItemElement = null)
    {
        createEmptyItemElement = createEmptyItemElement ?? CreateLabelVisualElement;

        Assert.IsNotNull(listView, "Missing ListView - do the strings in root.Q<>(\"here\") and the UXML match?");
        listView.makeItem = createEmptyItemElement;
        listView.fixedItemHeight = itemHeight;
        listView.style.flexGrow = 1.0f;

        listView.itemsSource = new List<TContents>();
        listView.bindItem = (e, i) => applyContentsToUi(e, (TContents)listView.itemsSource[i]);
    }

    /// <summary>
    /// Add data to the list view.
    /// </summary>
    public static void BindListView<TContents>(ListView listView, IEnumerable<TContents> contents)
    {
        // n.b: Replace the existing list's elements instead of rebinding a new list
        // Replacing the list with a new instance breaks the UI Unity-side
        Assert.IsNotNull(listView);
        Assert.IsNotNull(listView.itemsSource);
        Assert.IsTrue(listView.itemsSource is List<TContents>);
        Assert.IsNotNull(contents);
        listView.itemsSource.Clear();
        ((List<TContents>)listView.itemsSource).AddRange(contents);
        listView.Rebuild();
    }

    private static VisualElement CreateLabelVisualElement()
    {
        return new Label();
    }
}

}

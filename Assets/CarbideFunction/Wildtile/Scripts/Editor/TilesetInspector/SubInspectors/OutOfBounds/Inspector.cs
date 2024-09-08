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
internal class Inspector
{
    [SerializeField]
    private TransientData transientData;
    [SerializeField]
    private View view;
    [SerializeField]
    private Controller controller;

    public Inspector(VisualElement root, IInspectorAccess inspectorAccess)
    {
        transientData = new TransientData();
        view = new View(root);
        controller = new Controller(view, transientData, inspectorAccess);
    }

    public void Clear()
    {
        transientData.Clear();
    }

    public void UpdateVerticesFromConnectivity(ModelTileConnectivityCalculator.ModuleConnectivityData connectivityData, ModuleMesh mesh)
    {
        transientData.SetOutOfBoundsVertices(connectivityData.vertexIndicesOutsideOfBounds);
        transientData.SetVertexPositions(mesh?.vertices ?? new Vector3[]{});
    }

    public void UploadCurrentValuesToUi()
    {
        view.UploadCurrentValuesToUi(transientData);
    }

    public int SelectedVertexIndex => view.SelectedVertexIndex;
}
}

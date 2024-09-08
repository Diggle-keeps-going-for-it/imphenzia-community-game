using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

/// <summary>
/// Diagnostic drawer for any triangles that are on any of a module's faces.
///
/// Intended for use with <see cref="TriangleOnModuleFaceDetector"/> and <see cref="TrianglesOnModuleFaceMeshCreator"/>.
/// </summary>
internal class TrianglesOnModuleFaceDrawer : IDiagnosticsDrawer
{
    /// <summary>
    /// Create and start the drawer.
    /// </summary>
    /// <param name="mesh">The <b>highlight</b> mesh, not the module's mesh i.e. the mesh that was constructed by <see cref="TrianglesOnModuleFaceMeshCreator"/>.</param>
    /// <param name="rootElement">The UI element that contains the sliders controlling the highlight's properties.</param>
    public TrianglesOnModuleFaceDrawer(Mesh mesh, Vector3 tileDimensions, VisualElement rootElement)
    {
        this.mesh = mesh;
        this.propertyBlock = new MaterialPropertyBlock();
        this.tileDimensions = tileDimensions;

        borderWidthSlider = rootElement.Q<Slider>(TrianglesOnModuleFaceDrawerResourcesAccess.instance.resources.borderWidthSliderName);
        SetBorderWidth(borderWidthSlider.value);
        borderWidthSlider.RegisterCallback<ChangeEvent<float>>(OnBorderWidthSliderChanged);
        normalOffsetSlider = rootElement.Q<Slider>(TrianglesOnModuleFaceDrawerResourcesAccess.instance.resources.normalOffsetSliderName);
        SetNormalOffset(normalOffsetSlider.value);
        normalOffsetSlider.RegisterCallback<ChangeEvent<float>>(OnNormalOffsetSliderChanged);
    }

    private void OnBorderWidthSliderChanged(ChangeEvent<float> change)
    {
        SetBorderWidth(change.newValue);
        RepaintSceneViews();
    }

    private void SetBorderWidth(float borderWidth)
    {
        propertyBlock.SetFloat("_BorderWidth", borderWidth * SceneViewUiScale);
    }

    private void RepaintSceneViews()
    {
        foreach (var sceneView in SceneView.sceneViews)
        {
            ((SceneView)sceneView).Repaint();
        }
    }

    private void OnNormalOffsetSliderChanged(ChangeEvent<float> change)
    {
        SetNormalOffset(change.newValue);
        RepaintSceneViews();
    }

    private void SetNormalOffset(float normalOffset)
    {
        propertyBlock.SetFloat("_NormalOffset", normalOffset * SceneViewUiScale);
    }

    void IDiagnosticsDrawer.Draw(SceneView view)
    {
        Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, TriangleOnFaceMaterial, 0, view.camera, 0, propertyBlock, false, false, false);
        Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, BorderHighlightMaterial, 0, view.camera, 1, propertyBlock, false, false, false);

        Handles.color = Color.blue;
        Handles.DrawWireCube(Vector3.zero, tileDimensions);
    }

    void IDiagnosticsDrawer.Shutdown()
    {
        borderWidthSlider.UnregisterCallback<ChangeEvent<float>>(OnBorderWidthSliderChanged);
        normalOffsetSlider.UnregisterCallback<ChangeEvent<float>>(OnNormalOffsetSliderChanged);
    }

    private Mesh mesh;

    private MaterialPropertyBlock propertyBlock;
    private Vector3 tileDimensions;
    private float SceneViewUiScale => Mathf.Max(tileDimensions.x, tileDimensions.y);

    private Slider borderWidthSlider;
    private Slider normalOffsetSlider;

    private static Material TriangleOnFaceMaterial => TrianglesOnModuleFaceDrawerResourcesAccess.instance.resources.triangleOnFaceMaterial;
    private static Material BorderHighlightMaterial => TrianglesOnModuleFaceDrawerResourcesAccess.instance.resources.borderHighlightMaterial;
}

}

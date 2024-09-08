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
/// Manager and renderer for the missing tiles sub-window inside the <see cref="TilesetReportWindow"/>.
/// </summary>
internal class MissingTilesSubWindow
{
    [Serializable]
    public class Data
    {
        public Material cubesMaterial;
        public Material wallsMaterial;
    }

    /// <summary>
    /// Sets up the sub window for rendering immediately
    /// </summary>
    /// <param name="subWindow">The VisualElement that this missing tiles subwindow will render into.</param>
    public MissingTilesSubWindow()
    {
        renderer = new PreviewRenderUtility();
        renderer.camera.transform.position = new Vector3(0,0, -cameraDistance);
        renderer.camera.nearClipPlane = 7;
        renderer.camera.farClipPlane = 13;
        var desiredViewHeight = 2 * Mathf.Sqrt(3) + cameraFovExtra;
        var cameraFov = Mathf.Rad2Deg * Mathf.Atan(desiredViewHeight / cameraDistance);
        renderer.camera.fieldOfView = cameraFov;
    }

    /// <summary>
    /// Start rendering to the IMGUIContainer and reacting to clicks and drags on it.
    /// </summary>
    public void Bind(IMGUIContainer subWindow)
    {
        subWindowRoot = subWindow;

        subWindowRoot.onGUIHandler = OnGui;
    }

    // The voxel mesher creates grids with each tile on the integer coordinates, starting at 0,0,0.
    // We need to offset this model so the centre between 0 and 1 is at the origin
    private static readonly Vector3 modelOffset = new Vector3(-0.5f, -0.5f, -0.5f);
    /// <summary>
    /// Draws the passed in <paramref name="mesh"/> into the subwindow using the settings (materials) from <paramref name="staticData"/> and the instance local orbit coordinates (controlled by the user clicking and dragging their mouse).
    /// </summary>
    public void Render(Rect drawRect, Data staticData, Mesh mesh)
    {
        Assert.IsNotNull(staticData);
        renderer.BeginPreview(drawRect, GUIStyle.none);

        var rotation = GetObjectOrientationFromOrbitRotation();
        var rotatedModelOffset = rotation * modelOffset;

        if (mesh != null)
        {
            Assert.AreEqual(mesh.subMeshCount, 2);
            renderer.DrawMesh(mesh, rotatedModelOffset, rotation, staticData.cubesMaterial, 0);
            renderer.DrawMesh(mesh, rotatedModelOffset, rotation, staticData.wallsMaterial, 1);
        }

        renderer.camera.Render();
        var previewImage = renderer.EndPreview();
        GUI.DrawTexture(drawRect, previewImage);
    }

    /// <summary>
    /// This must be called before the last reference's release as the internal Unity renderer (<see href="https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/PreviewRenderUtility.cs">PreviewRenderUtility</see>) must be shutdown before domain reload. C#'s deferred garbage collector can destroy this after the domain reload, breaking Unity's requirements.
    /// </summary>
    public void Destroy()
    {
        renderer.Cleanup();
        renderer = null;

        // it is possible to construct this class without binding to the VisualElement
        if (subWindowRoot != null)
        {
            subWindowRoot = null;
        }
    }

    private void StartOrbiting(Vector2 mousePosition)
    {
        isOrbiting = true;
        this.mousePosition = mousePosition;
        subWindowRoot.MarkDirtyRepaint();
    }

    private void StopOrbiting()
    {
        isOrbiting = false;
    }

    private void Orbit(Vector2 newPosition)
    {
        var mouseDelta = newPosition - mousePosition;
        mousePosition = newPosition;

        var searchParameters = TilesetReportWindowParametersAccess.instance.Parameters.missingTileParameters;
        var orbitDelta = mouseDelta * searchParameters.mouseOrbitSpeed;

        orbitYaw += orbitDelta.x;
        orbitPitch += orbitDelta.y;
        orbitPitch = Math.Clamp(orbitPitch, minPitch, maxPitch);
    }

    private void OnGui()
    {
        HandleEvent(Event.current);
    }

    private void HandleEvent(Event e)
    {
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        switch (e.GetTypeForControl(controlId))
        {
            case EventType.MouseDown:
                if (e.button == 0)
                {
                    StartOrbiting(e.mousePosition);
                    GUIUtility.hotControl = controlId;
                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (isOrbiting)
                {
                    Orbit(e.mousePosition);
                    // on drag, force this object to repaint
                    // without this, the framerate is low and inconsistent as it's only triggered by user events (e.g. mousing over a new element, clicking on something)
                    subWindowRoot.MarkDirtyRepaint();
                }
                break;

            case EventType.MouseUp:
                if (e.button == 0)
                {
                    StopOrbiting();
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;

            default:
                break;
        }
    }

    private Quaternion GetObjectOrientationFromOrbitRotation()
    {
        return Quaternion.AngleAxis(orbitPitch, Vector3.right) * Quaternion.AngleAxis(orbitYaw, Vector3.up);
    }

    private IMGUIContainer subWindowRoot = null;
    private PreviewRenderUtility renderer = null;
    private bool isOrbiting = false;
    private Vector2 mousePosition = Vector2.zero;
    private float orbitYaw = 45f;
    private float orbitPitch = -30f;
    private const float cameraDistance = 10f;
    private const float cameraFovExtra = 1f;
    private const float minPitch = -90f;
    private const float maxPitch = 90f;
}

}

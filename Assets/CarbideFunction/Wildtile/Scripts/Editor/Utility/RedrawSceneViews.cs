using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    /// <summary>
    /// Contains helper methods for redrawing Unity <see href="https://docs.unity3d.com/ScriptReference/SceneView.html">SceneViews</see>.
    /// </summary>
    public static class RedrawSceneViews
    {
        /// <summary>
        /// Force Unity to redraw all scene views.
        ///
        /// This is designed to be used when you have changed a mesh and uploaded it to an object in the scene and want the user to see it immediately.
        /// </summary>
        public static void Redraw()
        {
            foreach (SceneView sceneView in SceneView.sceneViews)
            {
                sceneView.Repaint();
            }
        }
    }
}

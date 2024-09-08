using UnityEditor;

namespace CarbideFunction.Wildtile.Editor.TilesetInspectorDiagnosticsDrawer
{

internal interface IDiagnosticsDrawer
{
    void Draw(SceneView view);
    void Shutdown();
}

}

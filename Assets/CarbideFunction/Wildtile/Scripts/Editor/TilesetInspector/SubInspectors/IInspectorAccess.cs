using UnityEngine;

using CarbideFunction.Wildtile.Editor;

namespace CarbideFunction.Wildtile.Editor.SubInspectors
{

internal interface IInspectorAccess
{
    void RefreshDiagnosticsDrawer();
    void OpenSceneViewAndFocusOnBounds(Bounds frameBounds);

    TilesetImporterAsset TilesetImporterAsset {get;}
    Tileset Tileset {get;}

    TilesetInspectorStage Stage {get;}
    CurrentModule CurrentModule {get;}
}
}

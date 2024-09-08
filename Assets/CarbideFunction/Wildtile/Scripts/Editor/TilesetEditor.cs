using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    [CustomEditor(typeof(Tileset))]
    internal class TilesetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Use a TilesetImporter to edit this asset. Tileset assets contain processed data that is non-human-readable, modifying just the human-readable data (e.g. tile dimensions) will introduce errors when collapsing and welding.",
                MessageType.Info
            );

            if (GUILayout.Button("Open importer instructions in web browser"))
            {
                Application.OpenURL($"{DocsWebsite.DocumentationWebsiteRoot}articles/create_tileset_workflow.html#creating-a-wildtile-tileset-and-importer");
            }

            using (new EditorGUI.DisabledScope(true))
            {
                base.DrawDefaultInspector();
            }
        }
    }
}

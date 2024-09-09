using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PropSpawner))]
public class PropSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        if (GUILayout.Button("Force Regenerate"))
        {
            var spawner = (PropSpawner)target;
            spawner.Respawn();
        }
    }
}

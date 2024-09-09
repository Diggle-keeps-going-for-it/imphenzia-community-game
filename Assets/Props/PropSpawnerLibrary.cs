using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Prop Spawner Library", menuName = "Prop Spawner Library")]
public class PropSpawnerLibrary : ScriptableObject
{
    [Serializable]
    public class Option
    {
        public GameObject prefab;
        public float weight;
    }

    public List<Option> options;
    [Min(0f)] public float noSpawnWeight = 0f;
}

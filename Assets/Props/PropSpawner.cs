using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteAlways]
public class PropSpawner : MonoBehaviour
{
    [SerializeField] private PropSpawnerLibrary library;

    private void Start()
    {
        SpawnProp();
    }

    private void SpawnProp()
    {
        var spawnedProp = SpawnRandomProp();
#if UNITY_EDITOR
        if (!Application.IsPlaying(this))
        {
            if (spawnedProp != null)
            {
                spawnedProp.hideFlags = HideFlags.HideAndDontSave;
            }
        }
#endif
    }

    public void Respawn()
    {
#if UNITY_EDITOR
        if (!Application.IsPlaying(gameObject))
        {
            DestroyInEditor();
        }
        else
        {
            DestroyLive();
        }
#else
        DestroyLive();
#endif

        SpawnProp();
    }

    private void DestroyLive()
    {
        foreach (GameObject child in transform)
        {
            Destroy(child.gameObject);
        }
    }

#if UNITY_EDITOR
    private void DestroyInEditor()
    {
        while (transform.childCount > 0)
        {
            var count = transform.childCount;
            DestroyImmediate(transform.GetChild(0).gameObject);
            Assert.AreEqual(count - 1, transform.childCount);
        }
    }
#endif

    private GameObject SpawnRandomProp()
    {
        var option = PickOption();

        if (option != null)
        {
            var instance = Instantiate(option.prefab, transform);
            return instance;
        }

        return null;
    }

    private PropSpawnerLibrary.Option PickOption()
    {
        if (library == null || library.options.Count == 0)
        {
            return null;
        }

        var total = library.noSpawnWeight + library.options.Aggregate(0f, (accumulatingTotal, option) => accumulatingTotal + option.weight);
        var selectedValue = Random.Range(0f, total);

        foreach (var option in library.options)
        {
            selectedValue -= option.weight;
            if (selectedValue <= 0f)
            {
                return option;
            }
        }

        // the no-spawn has been selected

        return null;
    }
}

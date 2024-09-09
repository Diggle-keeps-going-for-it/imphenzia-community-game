using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class PropSpawner : MonoBehaviour
{
    [SerializeField] private PropSpawnerLibrary library;

    private void Start()
    {
        var spawnedProp = SpawnRandomProp();
#if UNITY_EDITOR
        if (!Application.IsPlaying(this))
        {
            spawnedProp.hideFlags = HideFlags.HideAndDontSave;
        }
#endif
    }

    private GameObject SpawnRandomProp()
    {
        var option = PickOption();

        // option can be null if the library is empty
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

        var total = library.options.Aggregate(0f, (accumulatingTotal, option) => accumulatingTotal + option.weight);
        var selectedValue = Random.Range(0f, total);

        foreach (var option in library.options)
        {
            total -= option.weight;
            if (total <= 0f)
            {
                return option;
            }
        }

        Debug.LogWarning("Failed to randomly select from library", gameObject);
        return null;
    }
}

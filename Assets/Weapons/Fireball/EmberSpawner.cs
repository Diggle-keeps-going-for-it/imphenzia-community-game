using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmberSpawner : MonoBehaviour
{
    [SerializeField] private int maxToSpawn;
    [SerializeField] private GameObject emberPrefab;

    [SerializeField][MinMaxSlider(Min = 0f, Max=10f)] private Vector2 ejectionVelocity;

    private void Start()
    {
        var numberToSpawn = Random.Range(0, maxToSpawn + 1);
        for (var spawnIndex = 0; spawnIndex < numberToSpawn; ++spawnIndex)
        {
            var instance = Instantiate(emberPrefab, transform.position, Random.rotation, parent:null);
            var body = instance.GetComponent<Rigidbody>();
            body.velocity = instance.transform.forward * Random.Range(ejectionVelocity.x, ejectionVelocity.y);
        }
    }
}

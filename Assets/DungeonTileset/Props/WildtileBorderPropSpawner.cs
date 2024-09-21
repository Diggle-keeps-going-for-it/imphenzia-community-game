using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class WildtileBorderPropSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;

    void Start()
    {
        if (ShouldSpawn())
        {
            Spawn();
        }
    }

    private void Spawn()
    {
        Instantiate(prefab, transform.position, transform.rotation, transform);
    }

    private bool ShouldSpawn()
    {
        if (transform.forward.z > 0f)
        {
            return true;
        }
        else if (transform.forward.z == 0f)
        {
            var right = CalculateRightVector();
            if (right.x >= 0f)
            {
                return true;
            }
        }

        return false;
    }

    private Vector3 CalculateRightVector()
    {
        return Vector3.Cross(transform.forward, transform.up);
    }
}

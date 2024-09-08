using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsPlayerInBounds : MonoBehaviour
{
    [SerializeField] private Transform player;

    private bool playerIsInTrigger = false;
    public bool PlayerIsInTrigger => playerIsInTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            playerIsInTrigger = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == player)
        {
            playerIsInTrigger = false;
        }
    }
}

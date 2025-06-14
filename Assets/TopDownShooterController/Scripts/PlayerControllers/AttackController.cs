using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AttackController : MonoBehaviour
{
    [SerializeField] private PlayerAttack playerAttack;

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            playerAttack.TryAttack();
        }
    }
}

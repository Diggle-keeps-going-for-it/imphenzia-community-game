using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorStartAttackTriggerName;
    [SerializeField] private float attackDuration;

    private bool isAttacking = false;

    public void TryAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(Attack());
        }
    }

    private IEnumerator Attack()
    {
        Debug.Log("Attacking");
        isAttacking = true;
        animator.SetTrigger(animatorStartAttackTriggerName);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string animatorStartAttackTriggerName;
    [SerializeField] private string animatorAttackTimeParameterName;
    [SerializeField] private string animatorEndAttackTriggerName;

    [SerializeField] [Min(0f)] private float windupDuration;
    [SerializeField] [Min(0f)] private float castDuration;
    [SerializeField] [Tooltip("How long to play the end of the cast animation for. Does not affect gameplay")] [Min(0f)] private float recoveryAnimationDuration;
    [SerializeField] [Tooltip("How long after firing before the next shot can be started")] [Min(0f)] private float recoveryDuration;

    [SerializeField] [Range(0f, 1f)] private float animationStartCastNormalizedTime;
    [SerializeField] [Range(0f, 1f)] private float animationAttackExtremeNormalizedTime;

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
        isAttacking = true;
        animator.SetTrigger(animatorStartAttackTriggerName);
        var startTime = Time.time;

        yield return PlayWindup(startTime);
        yield return PlayCast(startTime);
        var proportionOfRecoveryThatIsUninterruptible = recoveryDuration / recoveryAnimationDuration;
        var animationUninterruptibleRecoveryNormalizedTime = Mathf.Lerp(animationAttackExtremeNormalizedTime, 1f, proportionOfRecoveryThatIsUninterruptible);
        yield return PlayUninterruptibleRecovery(startTime, animationUninterruptibleRecoveryNormalizedTime);
        isAttacking = false;
        animator.SetTrigger(animatorEndAttackTriggerName);
        yield return PlayPostAttackRecoveryGameplay(startTime, animationUninterruptibleRecoveryNormalizedTime);
    }

    private IEnumerator PlayWindup(float startTime)
    {
        yield return PlayAnimatedAttackSection(startTime, windupDuration, 0f, animationStartCastNormalizedTime);
    }

    private IEnumerator PlayCast(float startTime)
    {
        yield return PlayAnimatedAttackSection(startTime + windupDuration, castDuration, animationStartCastNormalizedTime, animationAttackExtremeNormalizedTime);
    }

    private IEnumerator PlayUninterruptibleRecovery(float startTime, float animationUninterruptibleRecoveryNormalizedTime)
    {
        yield return PlayAnimatedAttackSection(startTime + windupDuration + castDuration, recoveryDuration, animationAttackExtremeNormalizedTime, animationUninterruptibleRecoveryNormalizedTime);
    }

    private IEnumerator PlayPostAttackRecoveryGameplay(float startTime, float animationUninterruptibleRecoveryNormalizedTime)
    {
        yield return PlayAnimatedAttackSection(startTime + windupDuration + castDuration + recoveryDuration, recoveryAnimationDuration - recoveryDuration, animationUninterruptibleRecoveryNormalizedTime, 1f);
    }

    private IEnumerator PlayAnimatedAttackSection(float sectionStartTime, float sectionDuration, float inAnimationStartNormalizedTime, float inAnimationEndNormalizedTime)
    {
        var sectionEndTime = sectionStartTime + sectionDuration;
        while (Time.time < sectionEndTime)
        {
            var timeSinceStart = Time.time - sectionStartTime;
            var inSectionProportion = timeSinceStart / sectionDuration;
            var inAnimationTime = Mathf.Lerp(inAnimationStartNormalizedTime, inAnimationEndNormalizedTime, inSectionProportion);
            animator.SetFloat(animatorAttackTimeParameterName, inAnimationTime);
            yield return 0;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class KillEmber : MonoBehaviour
{
    [SerializeField] [MinMaxSlider] private Vector2 timeout;
    [SerializeField] private float minimumSpeed;
    [SerializeField] private VisualEffect visualEffect;
    [SerializeField] private string visualEffectIsEmittingProperty = "IsEmitting";
    [SerializeField] private Rigidbody body;
    [SerializeField] private float killToDestroyDelay = 1f;

    private float timeAlive = 0f;
    private float instanceMaxLifetime;

    private void Start()
    {
        instanceMaxLifetime = Random.Range(timeout.x, timeout.y);
    }

    private void FixedUpdate()
    {
        timeAlive += Time.fixedDeltaTime;
        if (body.velocity.sqrMagnitude < minimumSpeed * minimumSpeed)
        {
            StartKillingEmber();
        }
        else if (timeAlive > instanceMaxLifetime)
        {
            StartKillingEmber();
        }
    }

    private void StartKillingEmber()
    {
        enabled = false;
        visualEffect.Stop();
        visualEffect.SetBool(visualEffectIsEmittingProperty, false);
        body.isKinematic = true;
        Destroy(gameObject, killToDestroyDelay);
    }
}

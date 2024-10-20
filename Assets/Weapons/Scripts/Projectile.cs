using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Projectile : MonoBehaviour
{
    [SerializeField] private ProjectileSettings settings;
    [SerializeField] private UnityEvent onProjectileStopped;
    [SerializeField] [Min(0f)] private float timeToKillAfterProjectileStopped = 1f;

    private Vector3 velocity;
    private Vector3 nextPosition;

    public void Initialize(Vector3 position, Vector3 velocity)
    {
        this.nextPosition = position;
        this.velocity = velocity;
    }

    private void Update()
    {
        InterpolateRenderPosition();
    }

    private void InterpolateRenderPosition()
    {
        var timeSinceLastFixedUpdate = Time.time - Time.fixedTime;
        var proportionThroughUpdate = timeSinceLastFixedUpdate / Time.fixedDeltaTime;
        var previousPosition = nextPosition - velocity;
        transform.position = Vector3.Lerp(previousPosition, nextPosition, proportionThroughUpdate);
    }

    private void FixedUpdate()
    {
        UpdateVelocity();
        var hasHit = CastAlongVelocity();

        if (!hasHit)
        {
            ApplyVelocity();
        }
    }

    private void ApplyVelocity()
    {
        nextPosition += velocity;
    }

    private bool CastAlongVelocity()
    {
        if (Physics.SphereCast(nextPosition, settings.Radius, velocity, out var hitInfo, velocity.magnitude, settings.LayerMask))
        {
            var hitPoint = nextPosition + velocity.normalized * hitInfo.distance;
            Explode(hitPoint);
            Destroy(gameObject, timeToKillAfterProjectileStopped);
            return true;
        }

        return false;
    }

    private void Explode(Vector3 hitPoint)
    {
        onProjectileStopped?.Invoke();
    }

    private void UpdateVelocity()
    {
        velocity += settings.GravityScale * Time.fixedDeltaTime * Physics.gravity;
    }
}

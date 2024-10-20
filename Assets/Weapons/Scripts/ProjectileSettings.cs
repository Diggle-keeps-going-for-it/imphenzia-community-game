using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Projectile Settings", menuName = "Projectile Settings")]
public class ProjectileSettings : ScriptableObject
{
    [SerializeField] private float gravityScale = 1f;
    public float GravityScale => gravityScale;

    [SerializeField] private float radius;
    public float Radius => radius;

    [SerializeField] private LayerMask layerMask;
    public LayerMask LayerMask => layerMask;
}

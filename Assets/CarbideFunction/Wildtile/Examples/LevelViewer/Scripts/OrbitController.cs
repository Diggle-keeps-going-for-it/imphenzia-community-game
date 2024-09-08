using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CarbideFunction.Wildtile;

public class OrbitController : MonoBehaviour
{
    [SerializeField]
    private OrbitControllerParameters orbitControllerParameters;

    [SerializeField]
    private Vector2 orbitSphereCoordinates;
    [SerializeField]
    private Vector2 targetOrbitSphereCoordinates;

    private void Awake()
    {
        targetOrbitSphereCoordinates = orbitSphereCoordinates = orbitControllerParameters.startingOrbitSphereCoordinates;
    }

    private void Update()
    {
        EaseToTargetCoordinates();
        ApplyCoordinatesToTransform();
    }

    private void EaseToTargetCoordinates()
    {
        var offset = targetOrbitSphereCoordinates - orbitSphereCoordinates;
        var offsetMagnitude = offset.magnitude;

        if (offsetMagnitude > 1E-5f)
        {
            var newMagnitude = offsetMagnitude * Mathf.Exp(- orbitControllerParameters.easeToTargetSpeed * Time.deltaTime);
            var newOffset = offset * newMagnitude / offsetMagnitude;
            orbitSphereCoordinates = targetOrbitSphereCoordinates - newOffset;
        }
    }

    private void ApplyCoordinatesToTransform()
    {
        transform.rotation = CalculateRotation();
    }

    private Quaternion CalculateRotation()
    {
        switch (orbitControllerParameters.rotationOrder)
        {
        case OrbitControllerParameters.RotationOrder.XThenY:
            return CalculateRotationXThenY();
        case OrbitControllerParameters.RotationOrder.YThenX:
            return CalculateRotationYThenX();
        default:
            return Quaternion.identity;
        }
    }

    private Quaternion CalculateRotationYThenX()
    {
        return CalculateYRotation() * CalculateXRotation();
    }

    private Quaternion CalculateRotationXThenY()
    {
        return CalculateXRotation() * CalculateYRotation();
    }

    private Quaternion CalculateXRotation()
    {
        return Quaternion.AngleAxis(orbitSphereCoordinates.y, Vector3.right);
    }

    private Quaternion CalculateYRotation()
    {
        return Quaternion.AngleAxis(orbitSphereCoordinates.x, Vector3.up);
    }

    public void ApplyDeltaToOrbitPosition(Vector2 delta)
    {
        targetOrbitSphereCoordinates = targetOrbitSphereCoordinates + Vector2.Scale(delta, orbitControllerParameters.inputDeltaScaling);
        targetOrbitSphereCoordinates.y = Mathf.Clamp(targetOrbitSphereCoordinates.y, orbitControllerParameters.minimumOrbitHeight, orbitControllerParameters.maximumOrbitHeight);
    }
}

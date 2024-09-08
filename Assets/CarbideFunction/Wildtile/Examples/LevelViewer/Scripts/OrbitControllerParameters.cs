using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

[CreateAssetMenu(menuName=MenuConstants.topMenuName + "Examples/Orbit Controller Parameters", order = MenuConstants.orderBase + 20, fileName="New Orbit Controller Parameters")]
public class OrbitControllerParameters : ScriptableObject
{
    [SerializeField]
    public float minimumOrbitHeight = 10f;
    [SerializeField]
    public float maximumOrbitHeight = 80f;
    [SerializeField]
    public Vector2 startingOrbitSphereCoordinates = new Vector2(45f, 30f);

    [SerializeField]
    public float easeToTargetSpeed = 1f;

    [SerializeField]
    public Vector2 inputDeltaScaling = -Vector2.one;

    public enum RotationOrder
    {
        XThenY,
        YThenX,
    }
    [SerializeField]
    public RotationOrder rotationOrder = RotationOrder.YThenX;
}

}

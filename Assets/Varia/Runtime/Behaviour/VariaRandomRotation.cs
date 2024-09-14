using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Varia
{

    /// <summary>
    /// Rotates the object randomly around a given axis.
    /// 
    /// Two sorts of rotation are supported:
    /// * Rotating around the axis (rolling) using <see cref="min"/> and <see cref="max"/>.
    /// * Rotating away from the axis (pitch / yaw) using <see cref="dispersionMin"/> and <see cref="dispersionMax"/>
    /// </summary>
    [AddComponentMenu("Varia/Varia Random Rotation")]
    public class VariaRandomRotation : VariaBehaviour
    {
        /// <summary>
        /// Point to keep fixed during rotation
        /// </summary>
        public Vector3 point;

        /// <summary>
        /// Local axis of rotations
        /// </summary>
        public Vector3 axis = Vector3.up;

        /// <summary>
        /// Min amount to rotate around the axis
        /// </summary>
        public float min = -90;


        /// <summary>
        /// Max amount to rotate around the axis
        /// </summary>
        public float max = 90;

        /// <summary>
        /// Min amount to rotate away from the axis.
        /// </summary>
        public float dispersionMin = 0;

        /// <summary>
        /// Max amount to rotate away from the axis.
        /// </summary>
        public float dispersionMax = 0;

        public override void Apply(VariaContext context)
        {
            var angle = Random.Range(min, max);
            transform.RotateAround(transform.TransformPoint(point), transform.TransformDirection(axis), angle);
            var dispersionAngle = Random.Range(dispersionMin, dispersionMax);
            var dispersionTheta = Random.Range(0, 360);
            var axis2 = Vector3.Cross(axis, Vector3.forward);
            if (axis2.sqrMagnitude == 0) axis2 = Vector3.Cross(axis, Vector3.right);
            axis2 = Quaternion.AngleAxis(dispersionTheta, axis) * axis2;
            transform.RotateAround(transform.TransformPoint(point), transform.TransformDirection(axis2), dispersionAngle);

            if (context.log)
            {
                Debug.Log(("RandomRotate", gameObject.GetNamePath(), angle, dispersionTheta, dispersionAngle));
            }
        }
    }
}
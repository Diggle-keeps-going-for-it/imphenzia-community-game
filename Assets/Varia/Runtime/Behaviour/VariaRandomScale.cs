using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Varia
{
    /// <summary>
    /// Changes transform.localScale randomly.
    /// </summary>
    [AddComponentMenu("Varia/Varia Random Scale")]
    public class VariaRandomScale : VariaBehaviour
    {
        /// <summary>
        /// If true, X,Y and Z are all scaled together, otherwise they are independently scaled.
        /// </summary>
        public bool linked = true;

        public float minX = 1;
        public float maxX = 1;
        public float minY = 1;
        public float maxY = 1;
        public float minZ = 1;
        public float maxZ = 1;

        /// <summary>
        /// The local point that should stay fixed while scaling
        /// </summary>
        public Vector3 scaleOrigin;

        public override void Apply(VariaContext context)
        {
            var scaleX = Random.Range(minX, maxX);
            var scaleY = linked ? scaleX : Random.Range(minY, maxY);
            var scaleZ = linked ? scaleX : Random.Range(minZ, maxZ);
            var scale = new Vector3(scaleX, scaleY, scaleZ);
            var localToParent = (transform.parent == null ? Matrix4x4.identity : transform.parent.worldToLocalMatrix) * transform.localToWorldMatrix;

            transform.localPosition -= localToParent.MultiplyVector(Vector3.Scale(scaleOrigin, scale) - scaleOrigin);
            transform.localScale = Vector3.Scale(transform.localScale, scale);

            if (context.log)
            {
                Debug.Log(("RandomScale", gameObject.GetNamePath(), scaleX, scaleY, scaleZ));
            }
        }
    }
}
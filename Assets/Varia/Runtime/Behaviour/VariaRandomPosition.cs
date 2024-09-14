using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Varia
{
    public enum RelativeTo
    {
        Local,
        Parent,
        World
    }

    /// <summary>
    /// Offsets the position, randomly
    /// </summary>
    [AddComponentMenu("Varia/Varia Random Position")]
    public class VariaRandomPosition : VariaBehaviour
    {
        /// <summary>
        /// Inidcates what space the offset should be performed in.
        /// </summary>
        public RelativeTo relativeTo;

        public float minX = 0;
        public float maxX = 0;
        public float minY = 0;
        public float maxY = 0;
        public float minZ = 0;
        public float maxZ = 0;

        public override void Apply(VariaContext context)
        {
            var x = Random.Range(minX, maxX);
            var y = Random.Range(minY, maxY);
            var z = Random.Range(minZ, maxZ);

            var v = new Vector3(x, y, z);

            switch (relativeTo)
            {
                case RelativeTo.Local:
                    transform.position += transform.TransformVector(v);
                    break;
                case RelativeTo.World:
                    transform.position += v;
                    break;
                case RelativeTo.Parent:
                    if (transform.parent == null)
                    {
                        transform.position += v;
                    }
                    else
                    {
                        transform.position += transform.parent.TransformVector(v);
                    }
                    break;
            }

            if (context.log)
            {
                Debug.Log(("RandomPosition", gameObject.GetNamePath(), x, y, z));
            }
        }
    }
}
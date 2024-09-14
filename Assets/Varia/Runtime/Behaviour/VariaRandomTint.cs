using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Varia
{
    /// <summary>
    /// Randomly sets the color of a MeshRenderer or SpriteRenderer component.
    /// </summary>
    [AddComponentMenu("Varia/Varia Random Tint")]
    public class VariaRandomTint : VariaBehaviour
    {
        /// <summary>
        /// Specifices the specific component to set the value on.
        /// </summary>
        public Component target;

        /// <summary>
        /// The name of the property on the target component.
        /// </summary>
        public string property;

        public bool relative = false;
        public int relativeParent = 0;

        public float hueMin = 0;
        public float hueMax = 0;
        public float saturationMin = 1;
        public float saturationMax = 1;
        public float valueMin = 1;
        public float valueMax = 1;
        public float alphaMin = 1;
        public float alphaMax = 1;

        public override void Apply(VariaContext context)
        {
            if (string.IsNullOrEmpty(property))
                return;

            var color = GetColor();

            if (color != null)
            {
                if (context.log)
                {
                    Debug.Log(("RandomTint", gameObject.GetNamePath(), color));
                }

                var mirror = VariaReflection.EvalExpressionOrThrow(target.GetType(), property);
                mirror.setValue(target, color.Value);

            }
        }

        public Object GetRelativeTarget()
        {
            if (relativeParent == 0)
                return target;

            var ancestor = target.transform;
            for (var i = 0; i < relativeParent; i++) ancestor = ancestor?.parent;
            var ancestorGameObject = ancestor?.gameObject;
            if(target is Component)
            {
                if (ancestorGameObject.TryGetComponent(target.GetType(), out var ancestorComponent))
                    return ancestorComponent;
                return null;
            }
            else
            { 
                return ancestorGameObject;
            }
        }

        public Color? GetBaseColor()
        {
            var relativeTarget = GetRelativeTarget();
            var mirror = VariaReflection.EvalExpression(relativeTarget?.GetType(), property);
            if (mirror == null)
                return null;
            return (Color)mirror.getValue(relativeTarget);
        }

        public Color? GetColor(bool force = false)
        {
            if (relative)
            {
                var baseColor = GetBaseColor();
                if (baseColor == null && force)
                {
                    baseColor = Color.white;
                }

                if (baseColor == null)
                    return null;

                Color.RGBToHSV(baseColor.Value, out var h, out var s, out var v);
                var color = Color.HSVToRGB(Modulo(h + Random.Range(hueMin, hueMax), 1), s + Random.Range(saturationMin, saturationMax), v + Random.Range(valueMin, valueMax));
                color.a = baseColor.Value.a * Random.Range(alphaMin, alphaMax);
                return color;
            }
            else
            {
                var color = Color.HSVToRGB(Modulo(Random.Range(hueMin, hueMax), 1), Random.Range(saturationMin, saturationMax), Random.Range(valueMin, valueMax));
                color.a = Random.Range(alphaMin, alphaMax);
                return color;
            }
        }

        private float Modulo(float v, float m)
        {
            return (v % m + m) % m;
        }
    }
}
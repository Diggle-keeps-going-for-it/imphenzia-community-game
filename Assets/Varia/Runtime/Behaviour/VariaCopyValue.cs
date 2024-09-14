using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Varia
{
    [AddComponentMenu("Varia/Varia Copy Value")]
    public class VariaCopyValue : VariaBehaviour
    {
        public UnityEngine.Object srcTarget;

        public string srcProperty;

        public UnityEngine.Object destTarget;

        public string destProperty;

        public override void Apply(VariaContext context)
        {
            var value = VariaReflection.GetValue(srcTarget, srcProperty);
            if (context.log)
            {
                Debug.Log($"Copying from {srcTarget} to {destTarget}: {value}");
            }
            VariaReflection.SetValue(destTarget, destProperty, value);
        }
    }
}

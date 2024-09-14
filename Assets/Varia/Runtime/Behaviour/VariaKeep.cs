using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Varia
{
    /// <summary>
    /// Destroys the GameObject if the conditions are *not* met.
    /// You should add conditions to this component or it is mostly useless.
    /// </summary>
    [AddComponentMenu("Varia/Varia Keep")]
    public class VariaKeep : VariaBehaviour
    {
        public VariaKeep()
        {
            conditionList.conditions = new List<VariaCondition>()
            {
                new VariaCondition
                {
                    conditionType = VariaConditionType.Random,
                    randomChance = 0.5f,
                }
            };
        }

        public override void Apply(VariaContext context)
        {
            if (context.log)
            {
                Debug.Log(("VariaDestroy", gameObject.GetNamePath()));
            }
        }
        public override void NoApply(VariaContext context)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
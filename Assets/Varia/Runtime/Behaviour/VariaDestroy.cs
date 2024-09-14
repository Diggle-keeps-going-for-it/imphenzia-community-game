using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Varia
{

    /// <summary>
    /// Destroys the game object.
    /// You should add conditions to this component or it is mostly useless.
    /// </summary>
    [AddComponentMenu("Varia/Varia Destroy")]
    public class VariaDestroy : VariaBehaviour
    {
        public VariaDestroy()
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
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
#if UNITY_EDITOR
            else if (context.recordUndo)
            {
                UnityEditor.Undo.DestroyObjectImmediate(gameObject);
            }
#endif
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
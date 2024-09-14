using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Varia
{
    [Serializable]
    public class WeightedGameObject
    {
        public GameObject gameObject;
        public float weight = 1;
    }

    /// <summary>
    /// Picks a random prefab from a list, and instantiates it with VariaUtils.Instantiate.
    /// This may recursively instantiate more objects itself, which is tracked as the "depth".
    /// After the instantiation, this object is deleted.
    /// 
    /// Warning: There are some issues with using a target that is a direct parent of this component. 
    /// To fix this, either make an extra prefab to avoid the issue, or use VariaUtils.Instantiate instead of normal instantiation.
    /// </summary>
    [AddComponentMenu("Varia/Varia Instantiate")]
    public class VariaInstantiate : VariaBehaviour
    {
        private static bool hasWarned;



        /// <summary>
        /// The list of game objects to instantiate, and their weights.
        /// You are recommended to only instantiate prefabs, or objects marked with <see cref="VariaPrototype"/>
        /// </summary>
        public List<WeightedGameObject> targets;


        /// <summary>
        /// If enabled, the <see cref="WeightedGameObject.weight"/> property alters the probabiliyt of picking that target.
        /// Otherwise, they are picked uniformly.
        /// </summary>
        public bool useWeights;

        /// <summary>
        /// If true, destroys the game object the VariaInstantiate component is on. 
        /// This can be used to make the instantiation work as a replacement instead.
        /// </summary>
        public bool thenDestroyThis;

        public VariaInstantiate()
        {
            conditionList.conditions = new List<VariaCondition>()
            {
                new VariaCondition
                {
                    conditionType = VariaConditionType.DepthFilter,
                    comparison = VariaComparison.LessThanOrEquals,
                    depth = 10
                }
            };
        }

        public override void Apply(VariaContext context)
        {
            // Pick target
            var r = UnityEngine.Random.value * targets.Sum(x => useWeights ? x.weight : 1.0f);
            GameObject target = null;
            foreach(var w in targets)
            {
                r -= useWeights ? w.weight : 1.0f;
                if(r <= 0)
                {
                    target = w.gameObject;
                    break;
                }
            }

            if(target == null)
            {
                return;
            }

            if (this.transform.IsChildOf(target.transform))
            {
                if (!hasWarned && Application.isPlaying)
                {
                    hasWarned = true;
                    Debug.LogWarning("Cannot instantiate recursively. Use VariaUtils.Instantiate.");
                }
                return;
            }
            GameObject created = null;
            if (context.log)
            {
                Debug.Log(("Instantiate", gameObject.GetNamePath(), context.depth));
                Debug.Log(("Begin creating", target.name, context.depth));

            }
            created = context.Instantiate(target, transform.position, transform.rotation, transform.parent);
            if (created != null)
            {
                created.transform.localScale = transform.localScale;
            }
            if(context.log)
            {
                Debug.Log(("End creating", target.name, context.depth));
            }
            if (thenDestroyThis)
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
}
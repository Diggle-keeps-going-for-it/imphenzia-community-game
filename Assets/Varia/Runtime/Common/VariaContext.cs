using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Varia
{
    public class VariaContext
    {
        private static List<VariaContext> stack = new List<VariaContext>() { 
            new VariaContext()
        };

        public static VariaContext current => stack[stack.Count - 1];

        private static void Push(VariaContext next)
        {
            var context = next;
            stack.Add(context);
        }

        private static void Pop()
        {
            stack.RemoveAt(stack.Count - 1);
        }

        public VariaContext()
        {
            randomState = Random.state;
            // Force randomness forwards
            var _ = Random.value;
        }

        public int depth { get; private set; }

        public bool log { get; set; } = true;
        public bool recordUndo { get; set; } = false;

        public Random.State randomState;

        public GameObject Instantiate(GameObject original, Transform parent, bool worldPositionStays)
        {
            return Instantiate(original, () => Object.Instantiate(original, parent, worldPositionStays));
        }
        public GameObject Instantiate(GameObject original, Transform parent)
        {
            return Instantiate(original, () => Object.Instantiate(original, parent));
        }
        public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
        {
            return Instantiate(original, () => Object.Instantiate(original, position, rotation, parent));
        }
        public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            return Instantiate(original, () => Object.Instantiate(original, position, rotation));
        }
        public GameObject Instantiate(GameObject original)
        {
            return Instantiate(original, () => Object.Instantiate(original));
        }

        private GameObject Instantiate(GameObject original, System.Func<GameObject> doInstantiate)
        {
            var newContext = new VariaContext
            {
                log = log,
                recordUndo = recordUndo,
                depth = depth + 1,
            };
            GameObject go;
            try
            {
                Push(newContext);
                go = doInstantiate();

#if UNITY_EDITOR
                if (recordUndo)
                {
                    UnityEditor.Undo.RegisterCreatedObjectUndo(go, UnityEditor.Undo.GetCurrentGroupName());
                }
#endif

                ClearPrototype(go);

                // Update any recursive references
                var newToOld = new Dictionary<GameObject, GameObject>();
                MakeRepoint(original, go, newToOld);
                RepointAll(go, newToOld);

                // When planing, Start() is automatically called, in Editor, we give it a little kick.
                if (!Application.isPlaying)
                {
                    StartAll(go, newContext);
                }
            }
            finally
            {
                Pop();
            }
            return go;
        }

        // Destroys all children marked with VariaPrototype.
        // TODO: I forget why this is needed?
        private static void ClearPrototype(GameObject newObject)
        {
            foreach (var prototype in newObject.GetComponents<VariaPrototype>())
            {
                prototype.enabled = false;
                if (Application.isPlaying)
                {
                    Object.Destroy(prototype);
                }
                else
                {
                    Object.DestroyImmediate(prototype);
                }
            }
            foreach (Transform child in newObject.transform)
            {
                ClearPrototype(child.gameObject);
            }
        }


        /// <summary>
        /// Finds a correspondance between the children of original and children of newObject, assuming newObject was just instantiated from original.
        /// </summary>
        private static void MakeRepoint(GameObject original, GameObject newObject, Dictionary<GameObject, GameObject> newToOldOut)
        {
            newToOldOut[newObject] = original;
            foreach (var _ in original.transform.Cast<Transform>().Zip(newObject.transform.Cast<Transform>(), (a, b) =>
             {
                 MakeRepoint(a.gameObject, b.gameObject, newToOldOut);
                 return 0;
             }))
            {

            }
        }

        /// <summary>
        /// Update references in children of an object according to the given map.
        /// </summary>
        private static void RepointAll(GameObject go, Dictionary<GameObject, GameObject> newToOld)
        {
            foreach (var c in go.GetComponents<VariaInstantiate>().ToList())
            {
                foreach(var wgo in c.targets)
                {
                    if (newToOld.TryGetValue(wgo.gameObject, out var ngo))
                    {
                        wgo.gameObject = ngo;
                    }
                }
            }
            foreach (Transform child in go.transform)
            {
                RepointAll(child.gameObject, newToOld);
            }
        }

        public void Freeze(GameObject go)
        {
            StartAll(go, this);
        }

        // Removes all VariaBehaviours
        private void ClearVaria(GameObject go)
        {
            foreach (var c in go.GetComponents<VariaBehaviour>().ToList())
            {
                if (Application.isPlaying)
                {
                    GameObject.Destroy(c);
                }
#if UNITY_EDITOR
                else if (recordUndo)
                {
                    UnityEditor.Undo.DestroyObjectImmediate(c);
                }
#endif
                else
                {
                    Object.DestroyImmediate(c);
                }
            }
            if (go == null)
                return;
            foreach (Transform child in go.transform.Cast<Transform>().ToList())
            {
                ClearVaria(child.gameObject);
            }
        }


        // Runs VariaBehaviour.Start in the same order that Unity would normally do so.
        private static void StartAll(GameObject go, VariaContext context)
        {
            foreach (var c in go.GetComponents<VariaBehaviour>().ToList())
            {
                context.ConditionalApply(c);
            }
            if (go == null)
                return;
            foreach (Transform child in go.transform.Cast<Transform>().ToList())
            {
                StartAll(child.gameObject, context);
            }
        }

        internal void ConditionalApply(VariaBehaviour c)
        {
            if (c == null)
            {
                return;
            }

            if(!c.enabled)
            {
                return;
            }


            // Set the Random state to something specific to this component.
            // This make the random generator more stable, as it is unaffected by how many random numbers
            // other components choose to read.
            var oldState = Random.state;
            Random.state = randomState;
            var newSeed = Random.Range(int.MinValue, int.MaxValue);
            randomState = Random.state;
            Random.InitState(newSeed);

            var passed = c.conditionList.conditions.All(CheckCondition);

            if (passed)
            {
                c.Apply(this);
            }
            else
            {
                c.NoApply(this);
            }

            // Don't run twice
            if (c != null)
            {
                c.enabled = false;
            }

            Random.state = oldState;
        }

        private bool CheckCondition(VariaCondition condition)
        {
            switch (condition.conditionType)
            {
                case VariaConditionType.Random:
                    return Random.value < condition.randomChance;
                case VariaConditionType.DepthFilter:
                    return Comparison(condition.comparison, this.depth, condition.depth);
                default:
                    throw new System.Exception($"Unknown condition type {condition.conditionType}");
            }
        }

        private bool Comparison(VariaComparison comparison, int a, int b)
        {
            switch (comparison)
            {
                case VariaComparison.LessThan:
                    return a < b;
                case VariaComparison.LessThanOrEquals:
                    return a <= b;
                case VariaComparison.GreaterThan:
                    return a > b;
                case VariaComparison.GreaterThanOrEquals:
                    return a >= b;
                case VariaComparison.Equals:
                    return a == b;
                case VariaComparison.NotEquals:
                    return a != b;
                default:
                    throw new System.Exception($"Unknown comparison {comparison}");
            }
        }
    }
}
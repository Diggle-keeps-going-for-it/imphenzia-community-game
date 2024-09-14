using UnityEngine;


namespace Varia
{
    public static class VariaUtils
    {
        /// <summary>
        /// Same behaviour as GameObject.Instantiate
        /// </summary>
        public static GameObject Instantiate(GameObject original, Transform parent, bool worldPositionStays)
        {
            return new VariaContext().Instantiate(original, parent, worldPositionStays);
        }

        /// <summary>
        /// Same behaviour as GameObject.Instantiate
        /// </summary>
        public static GameObject Instantiate(GameObject original, Transform parent)
        {
            return new VariaContext().Instantiate(original, parent);
        }

        /// <summary>
        /// Same behaviour as GameObject.Instantiate
        /// </summary>
        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
        {
            return new VariaContext().Instantiate(original, position, rotation, parent);
        }

        /// <summary>
        /// Same behaviour as GameObject.Instantiate
        /// </summary>
        public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            return new VariaContext().Instantiate(original, position, rotation);
        }

        /// <summary>
        /// Same behaviour as GameObject.Instantiate
        /// </summary>
        public static GameObject Instantiate(GameObject original)
        {
            return new VariaContext().Instantiate(original);
        }

        /// <summary>
        /// Gives the name of the all the ancestors of the current game object
        /// </summary>
        public static string GetNamePath(this GameObject go)
        {
            if(go.transform.parent == null)
            {
                return go.name;
            }
            else
            {
                return go.transform.parent.gameObject.GetNamePath() + " > " + go.name;
            }
        }
    }
}
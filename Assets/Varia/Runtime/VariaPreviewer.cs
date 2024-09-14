using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Varia
{
    /// <summary>
    /// Utility for automatically calling VariaUtils.Instantiate.
    /// This is particularly useful in the editor to get a live preview of results.
    /// </summary>
    [ExecuteAlways]
    public class VariaPreviewer : MonoBehaviour
    {
        private float lastRefresh;

        public bool continuousRefresh;

        public bool refreshInEditor;

        public float refreshBufferTime = 0.0f;

        public int seed = 0;

        public GameObject target;

        void Start()
        {
            lastRefresh = Time.realtimeSinceStartup;
            if (enabled && (Application.isPlaying || refreshInEditor))
            {
                Refresh();
            }
        }

        void Update()
        {
            if (!Application.isPlaying && enabled && continuousRefresh && refreshInEditor)
            {
                if (lastRefresh + refreshBufferTime < Time.realtimeSinceStartup)
                {
                    Refresh();
                }
            }
        }

        public void Refresh()
        {
            if (target != null)
            {
                foreach (Transform child in transform)
                {
                    DestroyImmediate(child.gameObject);
                }
                var oldState = Random.state;
                if (seed != 0)
                {
                    Random.InitState(seed);
                }

                var go = VariaContext.current.Instantiate(target, transform.position, transform.rotation, transform);
                if (go != null)
                {
                    go.hideFlags = HideFlags.DontSave;
                }
                if (seed != 0)
                {
                    Random.state = oldState;
                }
            }
            lastRefresh = Time.realtimeSinceStartup;
        }
    }
}
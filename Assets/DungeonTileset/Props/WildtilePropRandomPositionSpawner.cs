using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[ExecuteAlways]
public class WildtilePropRandomPositionSpawner : MonoBehaviour
{
    [SerializeField] private GameObject spanObject;

    [SerializeField] private GameObject prefab;

    private void Start()
    {
        var (instancePosition, instanceRotation) = GeneratePositionAndRotation();

        var instance = Instantiate(prefab, instancePosition, instanceRotation, transform);
        if (instance != null)
        {
            instance.hideFlags = HideFlags.DontSave;
        }
    }

    private (Vector3, Quaternion) GeneratePositionAndRotation()
    {
        if (spanObject != null)
        {
            var span = GetPositionSpan();
            var widthRandom = Random.value;
            var heightRandom = Random.value;
            var instancePosition = span.worldBasis + span.worldCrossOffset * widthRandom + span.worldUpOffset * heightRandom;
            var instanceRotation = Quaternion.Slerp(transform.rotation, spanObject.transform.rotation, widthRandom);
            return (instancePosition, instanceRotation);
        }
        else
        {
            return (transform.position, transform.rotation);
        }
    }

    public class PositionSpan
    {
        public Vector3 worldBasis;
        public Vector3 worldCrossOffset;
        public Vector3 worldUpOffset;
    }
    public PositionSpan GetPositionSpan()
    {
        Assert.IsNotNull(spanObject);
        var span = spanObject.transform.position - transform.position;
        return new PositionSpan{
            worldBasis = transform.position,
            worldCrossOffset = new Vector3(span.x, 0f, span.z),
            worldUpOffset = new Vector3(0f, span.y, 0f),
        };
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildtilePropRandomPositionSpawner : MonoBehaviour
{
    [SerializeField] private GameObject spanObject;

    [SerializeField] private GameObject prefab;

    private void Start()
    {
        var span = GetPositionSpan();
        var widthRandom = Random.value;
        var heightRandom = Random.value;
        var instancePosition = span.worldBasis + span.worldCrossOffset * widthRandom + span.worldUpOffset * heightRandom;
        var instanceRotation = Quaternion.Slerp(transform.rotation, spanObject.transform.rotation, widthRandom);

        Instantiate(prefab, instancePosition, instanceRotation, transform);
    }

    public class PositionSpan
    {
        public Vector3 worldBasis;
        public Vector3 worldCrossOffset;
        public Vector3 worldUpOffset;
    }
    public PositionSpan GetPositionSpan()
    {
        var span = spanObject.transform.position - transform.position;
        return new PositionSpan{
            worldBasis = transform.position,
            worldCrossOffset = new Vector3(span.x, 0f, span.z),
            worldUpOffset = new Vector3(0f, span.y, 0f),
        };
    }
}

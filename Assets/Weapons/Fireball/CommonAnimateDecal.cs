using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CommonAnimateDecal : MonoBehaviour
{
    [SerializeField] private AnimationCurve scale;
    [SerializeField] private AnimationCurve alpha;
    [SerializeField] [Min(0f)] private float duration = 1f;
    [SerializeField] private DecalProjector decal;
    [SerializeField] private string alphaPropertyName = "_Alpha";

    private float startTime;
    private Vector2 decalStartXySize;

    private void Start()
    {
        var materialInstance = Instantiate(decal.material);
        decal.material = materialInstance;

        decalStartXySize = new Vector2(decal.size.x, decal.size.y);

        startTime = Time.time;
    }

    private void Update()
    {
        var inAnimationTimeSinceStart = (Time.time - startTime) / duration;
        var nowScale = scale.Evaluate(inAnimationTimeSinceStart);
        var nowAlpha = alpha.Evaluate(inAnimationTimeSinceStart);

        decal.size = new Vector3(
            decalStartXySize.x * nowScale,
            decalStartXySize.y * nowScale,
            decal.size.z
        );
        decal.material.SetFloat(alphaPropertyName, nowAlpha);
    }
}

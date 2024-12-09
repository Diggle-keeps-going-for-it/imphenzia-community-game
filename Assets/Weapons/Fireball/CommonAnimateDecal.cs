using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CommonAnimateDecal : MonoBehaviour
{
    [SerializeField] private AnimationCurve scale;
    [SerializeField] private AnimationCurve alpha;
    [SerializeField] private DecalProjector decal;
    [SerializeField] private string scalePropertyName = "_Scale";
    [SerializeField] private string alphaPropertyName = "_Alpha";

    private float startTime;

    private void Start()
    {
        var materialInstance = Instantiate(decal.material);
        decal.material = materialInstance;

        startTime = Time.time;
    }

    private void Update()
    {
        var timeSinceStart = Time.time - startTime;
        var nowScale = scale.Evaluate(timeSinceStart);
        var nowAlpha = alpha.Evaluate(timeSinceStart);

        decal.material.SetFloat(scalePropertyName, nowScale);
        decal.material.SetFloat(alphaPropertyName, nowAlpha);
    }
}

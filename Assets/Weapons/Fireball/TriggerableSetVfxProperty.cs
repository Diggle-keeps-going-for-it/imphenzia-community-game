using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class TriggerableSetVfxProperty : MonoBehaviour
{
    [SerializeField] private VisualEffect effect;
    [SerializeField] private string propertyName;

    public void SetBoolPropertyOnVisualEffect(bool value)
    {
        effect.SetBool(propertyName, value);
    }
}

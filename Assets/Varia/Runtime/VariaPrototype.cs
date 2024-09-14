using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Varia
{
    /// <summary>
    /// Add this to any game objects with VariaBehaviours that you want to instantiate multiple times.
    /// It disables all VariaBehaviour on this object and children, so it is pristine for copying.
    /// It's not necessary for prefabs.
    /// </summary>
    public class VariaPrototype : MonoBehaviour
    {
    }
}
using System;
using UnityEngine;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{
[Serializable]
internal struct CurrentModule
{
    [SerializeField]
    public string name;
    public SelectedModule module;

    public CurrentModule(string name, SelectedModule module)
    {
        this.name = name;
        this.module = module;
    }
}
}

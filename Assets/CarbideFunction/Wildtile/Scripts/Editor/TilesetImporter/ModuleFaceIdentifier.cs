using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor
{

[Serializable]
internal struct ModuleFaceIdentifier
{
    public string module;
    public int prefabIndex;

    public bool flipped;
    public ModuleFaceIdentifier WithFlipped(bool flipped)
    {
        var result = this;
        result.flipped = flipped;
        return result;
    }

    public Face face;
    public ModuleFaceIdentifier WithFace(Face face)
    {
        var result = this;
        result.face = face;
        return result;
    }

    /// <summary>
    /// If this hash is for the matching faces for this face, not the face itself.
    /// </summary>
    public bool opposite;
    public ModuleFaceIdentifier WithOpposite(bool opposite)
    {
        var result = this;
        result.opposite = opposite;
        return result;
    }

    /// <summary>
    /// The yaw index of this face. Only used when the face is top or bottom.
    /// </summary>
    public int yawIndex;
    public ModuleFaceIdentifier WithYawIndex(int yawIndex)
    {
        var result = this;
        result.yawIndex = yawIndex;
        return result;
    }

    public override string ToString()
    {
        if (face == Face.Up || face == Face.Down)
        {
            return $"{module} {face} yaw {yawIndex * 90}{(flipped ? " flipped" : "")}{(opposite ? " opposite" : "")}";
        }
        else
        {
            return $"{module} {face}{(flipped ? " flipped" : "")}{(opposite ? " opposite" : "")}";
        }
    }
}

}

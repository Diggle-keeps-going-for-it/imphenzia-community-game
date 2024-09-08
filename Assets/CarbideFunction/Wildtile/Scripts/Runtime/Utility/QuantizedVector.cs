using System;
using UnityEngine;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Contains methods for converting a Unity float vector (e.g. <see href="https://docs.unity3d.com/ScriptReference/Vector3.html">Vector3</see>) into an in vector (e.g. <see href="https://docs.unity3d.com/ScriptReference/Vector3Int.html">Vector3Int</see>).
/// </summary>
public static class QuantizedVector
{
    /// <summary>
    /// Convert a 2D float vector into an int vector.
    /// Multiplies the vector components by <paramref name="quantizationResolution"/> before rounding to the nearest int. For vectors with 0-1 domains, this will convert the values to 0-<paramref name="quantizationResolution"/>.
    /// </summary>
    public static Vector2Int Quantize(this Vector2 vector, float quantizationResolution)
    {
        return Vector2Int.RoundToInt(vector * quantizationResolution);
    }

    /// <summary>
    /// Convert a 3D float vector into an int vector.
    /// Multiplies the vector components by <paramref name="quantizationResolution"/> before rounding to the nearest int. For vectors with 0-1 domains, this will convert the values to 0-<paramref name="quantizationResolution"/>.
    /// </summary>
    public static Vector3Int Quantize(this Vector3 vector, float quantizationResolution)
    {
        return Vector3Int.RoundToInt(vector * quantizationResolution);
    }
}

}

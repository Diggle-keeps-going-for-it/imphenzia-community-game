using System;
using UnityEngine;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class contains extension methods to print full precision vectors.
///
/// Unity's `ToString()` will format vector components to 2 decimal places.
/// These methods format the vector components to their maximum available precision, which is useful when debugging.
/// </summary>
public static class DetailedVectorString
{
    /// <summary>
    /// Return a full precision string representation of <paramref name="vector"/>
    /// </summary>
    public static string ToDetailedString(this Vector2 vector)
    {
        return $"({vector.x}, {vector.y})";
    }

    /// <summary>
    /// Return a full precision string representation of <paramref name="vector"/>
    /// </summary>
    public static string ToDetailedString(this Vector3 vector)
    {
        return $"({vector.x}, {vector.y}, {vector.z})";
    }
}

}

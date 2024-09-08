using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Extension methods for looking up elements in 3D C# arrays using Unity int-vectors.
/// </summary>
public static class ArrayLookup
{
    /// <summary>
    /// Find an element in a 3D array by Unity Vector3Int coordinates.
    /// </summary>
    public static ref ArrayContents Lookup<ArrayContents>(this ArrayContents[,,] sourceArray, Vector3Int coordinates)
    {
        return ref sourceArray[coordinates.x, coordinates.y, coordinates.z];
    }

    /// <summary>
    /// Test if a coordinate is within an array's dimensions.
    ///
    /// Note this does not check if there is an instance of coordinates in <paramref name="sourceArray"/>, like <see href="https://learn.microsoft.com/en-us/dotnet/api/system.array.system-collections-ilist-contains?view=net-7.0#system-array-system-collections-ilist-contains(system-object)">C# `Contains` methods</see>
    /// </summary>
    public static bool Contains<ArrayContents>(this ArrayContents[,,] sourceArray, Vector3Int coordinates)
    {
        return sourceArray.Dimensions().Contains(coordinates);
    }

    /// <summary>
    /// Get a 3D array's dimensions in Vector3Int form
    /// </summary>
    public static Vector3Int Dimensions<ArrayContents>(this ArrayContents[,,] sourceArray)
    {
        return new Vector3Int(
            sourceArray.GetLength(0),
            sourceArray.GetLength(1),
            sourceArray.GetLength(2)
        );
    }

    /// <summary>
    /// Test if a coordinate is within the supplied dimensions.
    ///
    /// Note this does not check if there is an instance of coordinates in <paramref name="sourceArray"/>, like <see href="https://learn.microsoft.com/en-us/dotnet/api/system.array.system-collections-ilist-contains?view=net-7.0#system-array-system-collections-ilist-contains(system-object)">C# `Contains` methods</see>
    /// </summary>
    public static bool Contains(this Vector3Int dimensions, Vector3Int coordinates)
    {
        return
            coordinates.x >= 0 && dimensions.x > coordinates.x &&
            coordinates.y >= 0 && dimensions.y > coordinates.y &&
            coordinates.z >= 0 && dimensions.z > coordinates.z;
    }
}

}

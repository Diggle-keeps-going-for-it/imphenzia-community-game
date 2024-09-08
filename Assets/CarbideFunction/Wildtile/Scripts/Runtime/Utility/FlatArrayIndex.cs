using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This extension class contains methods for interacting with a flat array as though it were a three dimensional array.
///
/// <see href="https://docs.unity3d.com/Manual/script-Serialization.html#FieldSerliaized2">Unity-serializable arrays can only be 1 dimensional</see> - this class allows the user to interact with them in a 3D context.
/// </summary>
public static class FlatArrayIndex
{
    /// <summary>
    /// Convert a 3D coordinate into a flat array index given the grid dimensions
    /// </summary>
    /// <param name="coordinate">The 3D coordinates of the query point. All components must be less than their corresponding <paramref name="dimensions"/> component</param>
    /// <param name="dimensions">The 3D dimensions of the grid</param>
    public static int ToFlatArrayIndex(this Vector3Int coordinate, Vector3Int dimensions)
    {
        return coordinate.x
            + coordinate.y * dimensions.x
            + coordinate.z * dimensions.x * dimensions.y;
    }

    /// <summary>
    /// Convert a 1D flat array index into a 3D coordinate given the grid dimensions.
    ///
    /// This assumes the flat array index is within the dimensions, no checks are made. The result is undefined if the flat index is not in the range [0- dimensions.x * y * z).
    /// </summary>
    public static Vector3Int To3dCoordinate(this int index, Vector3Int dimensions)
    {
        var xyLayerSize = dimensions.x * dimensions.y;
        var z = index / xyLayerSize;
        var y = (index - xyLayerSize * z) / dimensions.x;
        var x = (index - xyLayerSize * z - y * dimensions.x);
        return new Vector3Int(x, y, z);
    }

    /// <summary>
    /// Calculate the size of the flat array that would match the number of elements in a 3D grid of size <paramref name="dimensions"/>
    /// </summary>
    public static int GetFlatArraySize(this Vector3Int dimensions)
    {
        return dimensions.x * dimensions.y * dimensions.z;
    }
}

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

using IntegerType = System.UInt32;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Contains an index into the tileset's list of faces, uniquely referencing a face layout (the positions, normals, edges, and materials for all vertices touching the face of the tile cube).
///
/// Because this is an index, you can use it to access face data by array offset, an O(1) operation.
///
/// This array index works for all arrays of face data, both tileset (e.g. finding the matching face) and slot data (e.g. reading and updating the number of faces left for a face)
/// </summary>
[Serializable]
public struct FaceLayoutIndex
{
    [SerializeField]
    private IntegerType index;

    /// <summary>
    /// Readonly access to the underlying int value for this face layout. This value is unique to this face layout in this tileset.
    /// </summary>
    public IntegerType Index => index;

    /// <summary>
    /// Static access to the <see cref="index"/> field. Used in Unity serialization to read/write to the field, while keeping the field private in C#.
    /// </summary>
    public const string indexName = nameof(index);

    /// <summary>
    /// Test if another object is a <see cref="FaceLayoutIndex"/> and is equal to this object.
    /// </summary>
    public override bool Equals(object otherIndexObject)
    {
        if (otherIndexObject is FaceLayoutIndex otherIndex)
        {
            return Equals(otherIndex);
        }

        return false;
    }

    /// <summary>
    /// Test if another object is equal to this object.
    /// </summary>
    public bool Equals(FaceLayoutIndex otherIndex)
    {
        return index == otherIndex.index;
    }

    /// <summary>
    /// Returns the face layout index. Required as part of the override for <see cref="Equals"/>.
    /// </summary>
    public override int GetHashCode()
    {
        return (int)index;
    }

    /// <summary>
    /// Creates a new FaceLayoutIndex with the given <paramref name="faceIndex"/>.
    /// </summary>
    public static FaceLayoutIndex FromRawInt(IntegerType faceIndex)
    {
        return new FaceLayoutIndex{
            index = faceIndex
        };
    }

    /// <summary>
    /// Returns the face index as a string (e.g. `Face Index 5`).
    ///
    /// Intended to be used for debugging only.
    /// </summary>
    public override string ToString()
    {
        return $"Face Index {index}";
    }
}

}

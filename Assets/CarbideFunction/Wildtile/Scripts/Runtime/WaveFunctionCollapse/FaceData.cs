using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class contains information for iterating through the <see cref="Face">faces</see> of a cube.
/// </summary>
public static class FaceDataSerialization
{
    /// <summary>
    /// A detailed description for a single face of a cube, indicated by the <see cref="Face"/> enum.
    /// </summary>
    public struct SerializationFace
    {
        /// <summary>
        /// The <see cref="Face"/> enum that this SerializationFace is describing.
        /// </summary>
        public Face face;

        /// <summary>
        /// The property name of this face in <see name="FaceData{ContainedData}"/> classes. This can be used to serialize data to the FaceData's faces.
        /// </summary>
        public string facePropertyName;

        /// <summary>
        /// The list of bit indices that this face uses to describe its fullness. Useful when checking if two modules should connect along a shared face.
        /// </summary>
        public int[] contentsBitIndices;
    }

    /// <summary>
    /// A static list of the 6 faces of a cube, fully described.
    /// </summary>
    // 'int' is used here as a dummy so I can instantiate the type to get at the name.
    // The names of the properties should be the same on all types.
    public static readonly SerializationFace[] serializationFaces = new SerializationFace[]{
        new SerializationFace{face = Face.Forward, facePropertyName = nameof(FaceData<int>.forward), contentsBitIndices = new []{4,5,6,7}},
        new SerializationFace{face = Face.Right,  facePropertyName = nameof(FaceData<int>.right),  contentsBitIndices = new []{1,3,5,7}},
        new SerializationFace{face = Face.Back, facePropertyName = nameof(FaceData<int>.back), contentsBitIndices = new []{0,1,2,3}},
        new SerializationFace{face = Face.Left,  facePropertyName = nameof(FaceData<int>.left),  contentsBitIndices = new []{0,2,4,6}},
        new SerializationFace{face = Face.Up,    facePropertyName = nameof(FaceData<int>.up),    contentsBitIndices = new []{2,3,6,7}},
        new SerializationFace{face = Face.Down,  facePropertyName = nameof(FaceData<int>.down),  contentsBitIndices = new []{0,1,4,5}},
    };

    /// <summary>
    /// Find a <see name="SerializationFace"/> from a <see name="Face"/>.
    /// </summary>
    public static SerializationFace ToSerializationFace(Face face)
    {
        return Array.Find(serializationFaces, serializationFace => serializationFace.face == face);
    }

    /// <summary>
    /// Convert bit indices into a bitmask.
    ///
    /// This method is designed for use with <see name="SerializationFace.contentsBitIndices"/>.
    /// </summary>
    public static int ToMask(int[] contentsBitIndices)
    {
        return contentsBitIndices.Aggregate(0, (currentAggregate, bitIndex) => currentAggregate + (1 << bitIndex));
    }

    /// <summary>
    /// List all the marching cube indices that would match with the <paramref name="wholeModuleVertexContents"/> marching cube index along <paramref name="thisModuleFace"/>.
    ///
    /// This enumeration will always have 16 elements.
    /// </summary>
    public static IEnumerable<int> EnumerateMatchingVertexContents(int wholeModuleVertexContents, FaceDataSerialization.SerializationFace thisModuleFace)
    {
        var oppositeModuleFace = Array.Find(FaceDataSerialization.serializationFaces, val => val.face == FaceDataSerialization.GetOppositeFace(thisModuleFace.face));

        var otherMarchingCubeBase = Enumerable.Range(0, 4)
            .Select(bitIndex => ((wholeModuleVertexContents >> thisModuleFace.contentsBitIndices[bitIndex]) & 1) << oppositeModuleFace.contentsBitIndices[bitIndex])
            .Sum();

        foreach (var farSideIndex in Enumerable.Range(0, 1 << 4))
        {
            var farSideValue = 0;
            foreach (var bitIndex in Enumerable.Range(0, 4))
            {
                var currentBit = (farSideIndex >> bitIndex) & 1;
                var desintationBit = 1 << thisModuleFace.contentsBitIndices[bitIndex];
                farSideValue += desintationBit * currentBit;
            }
            yield return otherMarchingCubeBase + farSideValue;
        }
    }

    /// <summary>
    /// Look up the opposite face for <paramref name="face"/>.
    /// </summary>
    public static Face GetOppositeFace(Face face)
    {
        switch (face)
        {
        case Face.Forward:
            return Face.Back;
        case Face.Right:
            return Face.Left;
        case Face.Back:
            return Face.Forward;
        case Face.Left:
            return Face.Right;
        case Face.Up:
            return Face.Down;
        case Face.Down:
            return Face.Up;
        default:
            throw new ArgumentException($"Input face {face} was not in the Face enum");
        }
    }

    /// <summary>
    /// Look up the face if the viewer rotated around the cube clockwise from above.
    ///
    /// Up and down faces are unchanged.
    /// </summary>
    public static Face GetNextClockwiseFace(Face face)
    {
        switch (face)
        {
        case Face.Forward:
            return Face.Right;
        case Face.Right:
            return Face.Back;
        case Face.Back:
            return Face.Left;
        case Face.Left:
            return Face.Forward;
        case Face.Up:
            return Face.Up;
        case Face.Down:
            return Face.Down;
        default:
            throw new ArgumentException($"Input face {face} was not in the Face enum");
        }
    }

    /// <summary>
    /// Look up the face if the viewer rotated around the cube clockwise <paramref name="numberOfSteps"/> from above.
    ///
    /// Up and down faces are unchanged.
    /// </summary>
    public static Face GetClockwiseFace(Face face, int numberOfSteps)
    {
        var minimalNumberOfSteps = numberOfSteps.PositiveModulo(4);

        var currentFace = face;
        for (var step = 0; step < minimalNumberOfSteps; ++step)
        {
            currentFace = GetNextClockwiseFace(currentFace);
        }

        return currentFace;
    }
}

/// <summary>
/// A generic struct that contains an instance of the specified type for each of the 6 faces on a cube.
/// </summary>
[Serializable]
public struct FaceData<ContainedData>
{
    [SerializeField]
    public ContainedData forward;
    [SerializeField]
    public ContainedData right;
    [SerializeField]
    public ContainedData back;
    [SerializeField]
    public ContainedData left;
    [SerializeField]
    public ContainedData up;
    [SerializeField]
    public ContainedData down;

    /// <summary>
    /// Create a new FaceData from a set of simple parameters and a converter that produces a more complex type.
    ///
    /// This is intended to reduce boilerplate when generating FaceData for strongly typed ints e.g. <see cref="FaceLayoutIndex"/>.
    /// </summary>
    /// <example>
    /// In this example we can reduce boilerplate and increase readability when generating a FaceData&lt;<see cref="FaceLayoutIndex"/>&gt;:
    /// <code>
    /// var faceLayouts = FaceData&lt;FaceLayoutIndex&gt;.Create(
    ///     rawValue => FaceLayoutIndex.FromRawInt(rawValue),
    ///     1,
    ///     2,
    ///     3,
    ///     4,
    ///     5,
    ///     6
    /// );
    /// </code>
    /// </example>
    public static FaceData<ContainedData> Create<InitType>(Func<InitType, ContainedData> converter,
        InitType northInit,
        InitType eastInit,
        InitType southInit,
        InitType westInit,
        InitType upInit,
        InitType downInit
    )
    {
        return new FaceData<ContainedData>{
            forward = converter(northInit),
            right = converter(eastInit),
            back = converter(southInit),
            left = converter(westInit),
            up = converter(upInit),
            down = converter(downInit),
        };
    }

    /// <summary>
    /// Create a new FaceData, with each face taking a new value from <paramref name="creator"/>.
    ///
    /// This is intended to allow you to generate a new instance of ContainedData for each face, for example you can create an empty list for each face of the new FaceData.
    /// </summary>
    public static FaceData<ContainedData> Create(Func<ContainedData> creator)
    {
        return Create(_ => creator(), 0,1,2,3,4,5);
    }

    /// <summary>
    /// Create a new FaceData from another FaceData with a different generic specialisation.
    /// Each face will be passed through the <paramref name="converter"/> before being stored in the same face on the new FaceData.
    /// </summary>
    public static FaceData<ContainedData> Create<InitType>(Func<InitType, ContainedData> converter, FaceData<InitType> source)
    {
        var result = new FaceData<ContainedData>();
        foreach (var face in FaceDataSerialization.serializationFaces)
        {
            result[face.face] = converter(source[face.face]);
        }
        return result;
    }

    /// <summary>
    /// Get or set the data instance corresponding to a specific face.
    /// </summary>
    public ContainedData this[Face face]
    {
        get => GetFace(face);
        set => SetFace(face, value);
    }

    /// <summary>
    /// Get the data instance corresponding to a specific face.
    /// </summary>
    public ContainedData GetFace(Face face)
    {
        switch (face)
        {
        case Face.Forward:
            return forward;
        case Face.Right:
            return right;
        case Face.Back:
            return back;
        case Face.Left:
            return left;
        case Face.Up:
            return up;
        case Face.Down:
            return down;
        default:
            var errorMessage = String.Format("Unknown face when getting data: {0}", face);
            Assert.IsTrue(false, errorMessage);
            throw new ArgumentException(errorMessage);
        }
    }

    /// <summary>
    /// Set the data instance corresponding to a specific face.
    /// </summary>
    public void SetFace(Face face, ContainedData newValue)
    {
        // need the duplicated switch statement because c# doesn't allow
        // refs from structs
        switch (face)
        {
        case Face.Forward:
            forward = newValue;
            break;
        case Face.Right:
            right = newValue;
            break;
        case Face.Back:
            back = newValue;
            break;
        case Face.Left:
            left = newValue;
            break;
        case Face.Up:
            up = newValue;
            break;
        case Face.Down:
            down = newValue;
            break;
        default:
            Assert.IsTrue(false, $"Unknown face when getting data: {face}");
            throw new ArgumentException($"Unknown face when getting data: {face}");
        }
    }
}

}

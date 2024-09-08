using System;
using UnityEngine;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This static class contains functions for calculating hashes from different Unity types.
///
/// All functions are deterministic and are well spread across the output range. This is not secure for cryptography.
///
/// Vectors are quantized before hashing - for each component of the vector, work out the closest fraction the number is and use that to seed the hashing algorithm. The size of the fraction is controlled by the <paramref name="quantizationResolution"/> parameter.
/// </summary>
public static class Hash
{
    private const int intWidth = 32;

    /// <summary>
    /// Hash a Vector2. Both components should be between -1 and 1 for a good hash spread, but all values can be hashed with a potentially poorer spread.
    /// </summary>
    /// <param name="vector">The vector to hash</param>
    /// <param name="quantizationResolution">How finely should the vector be split up before hashing.</param>
    public static IntegerType QuantizedVector(Vector2 vector, float quantizationResolution)
    {
        var quantized = vector.Quantize(quantizationResolution);
        return CombineVectorHashes(Int(quantized.x), Int(quantized.y));
    }

    internal static IntegerType CombineVectorHashes(IntegerType x, IntegerType y)
    {
        var shiftedX = x;
        var shiftedY = CycleShift(y, 1);
        return CycleShift(shiftedX, shiftedY) ^ shiftedY;
    }

    internal static IntegerType CombineVectorHashes(IntegerType x, IntegerType y, IntegerType z)
    {
        var shiftedX = x;
        var shiftedY = CycleShift(y, 1);
        var shiftedZ = CycleShift(z, 2);
        return CycleShift(CycleShift(shiftedX, shiftedY) ^ shiftedY, shiftedZ) ^ shiftedZ;
    }

    /// <summary>
    /// Hash a Vector3. All components should be between -1 and 1 for a good hash spread, but all values can be hashed with a potentially poorer spread.
    /// </summary>
    /// <param name="vector">The vector to hash</param>
    /// <param name="quantizationResolution">How finely should the vector be split up before hashing.</param>
    public static IntegerType QuantizedVector(Vector3 vector, float quantizationResolution)
    {
        var quantized = vector.Quantize(quantizationResolution);
        return CombineVectorHashes(Int(quantized.x), Int(quantized.y), Int(quantized.z));
    }

    /// <summary>
    /// Hash an int.
    /// </summary>
    public static IntegerType Int(IntegerType seed)
    {
        // from https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
        var accumulator = seed + salt;
        accumulator = ((accumulator >> 16) ^ accumulator) * 0x45d9f3b;
        accumulator = ((accumulator >> 16) ^ accumulator) * 0x45d9f3b;
        accumulator =  (accumulator >> 16) ^ accumulator;
        return accumulator;
    }

    private const IntegerType salt = 0x1e_50_ab_a7;

    /// <summary>
    /// Left-shift bits around a word, with no loss of data. All bits that flow off either end are added to the opposite end.
    ///
    /// This allows the combination of hashes of several similar inputs by avoiding the collision of equal hashes. In particular, this is designed to support hashing <see href="https://docs.unity3d.com/ScriptReference/Vector2.html">Vector2s</see> and <see href="https://docs.unity3d.com/ScriptReference/Vector3.html">Vector3s</see> that should all be between -1 and 1.
    /// </summary>
    public static IntegerType CycleShift(IntegerType input, int leftShiftAmount)
    {
        var smallestPositiveLeftShiftAmount = leftShiftAmount.PositiveModulo(intWidth);

        return (input << smallestPositiveLeftShiftAmount) | (input >> (intWidth - smallestPositiveLeftShiftAmount));
    }

    /// <summary>
    /// Hash a string.
    /// </summary>
    public static IntegerType String(string input)
    {
        var aggregateResult = 0;
        foreach (char character in input)
        {
            aggregateResult = Int(aggregateResult ^ character);
        }
        return aggregateResult;
    }

    /// <summary>
    /// Hash a long to an int.
    /// </summary>
    public static IntegerType Long(long input)
    {
        var intMask = 0xFFFF;
        var lsb = (int)((input      ) & intMask);
        var msb = (int)((input >> 16) & intMask);
        return Int(lsb) ^ Int(msb);
    }
}

}

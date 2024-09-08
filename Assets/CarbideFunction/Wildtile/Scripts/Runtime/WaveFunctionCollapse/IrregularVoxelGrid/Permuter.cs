using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{
internal static class Permuter
{
    public static T[] GetPermutation<T>(T[] originalArray, int permutationIndex)
    {
        var clonedArray = (T[])originalArray.Clone();
        Assert.IsTrue(permutationIndex < Factorial(originalArray.Length));

        var permutationsRemaining = permutationIndex + 1;
        DoPermute(clonedArray, 0, ref permutationsRemaining);

        return clonedArray;
    }

    static void DoPermute<T>(T[] modifiableArray, int start, ref int permutationsRemaining)
    {
        if (start == modifiableArray.Length - 1)
        {
            // We have one of our possible n! solutions,
            //
            // add it to the list.
            --permutationsRemaining;
        }
        else
        {
            if (permutationsRemaining != 0)
            {
                for (var i = start; i < modifiableArray.Length; i++)
                {
                    Swap(ref modifiableArray[start], ref modifiableArray[i]);
                    DoPermute(modifiableArray, start + 1, ref permutationsRemaining);
                    if (permutationsRemaining == 0)
                    {
                        break;
                    }
                    Swap(ref modifiableArray[start], ref modifiableArray[i]);
                }
            }
        }
    }

    static void Swap<T>(ref T a, ref T b)
    {
        var temp = a;
        a = b;
        b = temp;
    }

    public static int Factorial(int input)
    {
        if (input <= 1)
        {
            return input;
        }
        else
        {
            return Factorial(input - 1) * input;
        }
    }
}
}

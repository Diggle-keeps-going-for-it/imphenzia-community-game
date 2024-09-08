using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile
{

[Serializable]
internal struct FaceLayoutHash
{
    [SerializeField]
    private IntegerType hashValue;
    public IntegerType HashValue => hashValue;
    public const string hashValueName = nameof(hashValue);

    public override bool Equals(object otherHashObject)
    {
        if (otherHashObject is FaceLayoutHash otherHash)
        {
            return Equals(otherHash);
        }

        return false;
    }

    public bool Equals(FaceLayoutHash otherHash)
    {
        return hashValue == otherHash.hashValue;
    }

    public override int GetHashCode()
    {
        return (int)hashValue;
    }

    public static FaceLayoutHash FromRawInt(int hashValue)
    {
        return new FaceLayoutHash{
            hashValue = hashValue
        };
    }

    public override string ToString()
    {
        return $"Hash {hashValue}";
    }
}

}

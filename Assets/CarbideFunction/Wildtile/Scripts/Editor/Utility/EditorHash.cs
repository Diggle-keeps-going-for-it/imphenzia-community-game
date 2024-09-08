using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using IntegerType = System.Int32;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Methods for hashing editor specific objects.
/// </summary>
public static class EditorHash
{
    /// <summary>
    /// Hash an asset <b>reference</b>. Deterministic across editor sessions.
    ///
    /// This does not hash the asset itself, only which asset this reference is pointing at.
    ///
    /// If the target is not an asset, this call will silently return 0.
    /// </summary>
    public static IntegerType AssetReference(UnityEngine.Object target)
    {
        if (target == null)
        {
            return 0;
        }

        var successfulLookup = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long localId);
        if (!successfulLookup)
        {
            return 0;
        }
        else
        {
            return Hash.Int(Hash.String(guid) ^ Hash.Long(localId));
        }
    }
}

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class is the actual module in terms of WFC. It reads from Wildtile <see cref="Module">aaaaaaa Modules</see> and transforms the referenced module's faces for easy access, while maintaining a single source of data.
/// </summary>
[Serializable]
public class TransformedModule
{
    [SerializeField]
    public int moduleIndex;

    [SerializeField]
    public bool isFlipped;
    [SerializeField]
    [Range(0,3)] // note this is fully open range, so includes 0,1,2,3
    public int yawIndex;

    [SerializeField]
    public float selectionWeight = 1f;


    /// <summary>
    /// Read face data from from Wildtile <see cref="Module">Modules</see> and get the relevant faces for <paramref name="isFlipped"/> and <paramref name="yawIndex"/>.
    /// 
    /// This method allocates memory.
    /// </summary>
    public static FaceData<FaceLayoutIndex> GetFaceLayoutIndices(Module module, bool isFlipped, int yawIndex)
    {
        if (isFlipped)
        {
            return GetYawedFaceIndex(module.flippedFaceIndices, yawIndex);
        }
        else
        {
            return GetYawedFaceIndex(module.faceIndices, yawIndex);
        }
    }

    /// <summary>
    /// Read face data from from Wildtile <see cref="Module">Modules</see> and get the relevant faces for this TransformedModule's transform.
    /// 
    /// This method allocates memory.
    /// </summary>
    public FaceData<FaceLayoutIndex> GetFaceLayoutIndices(IReadOnlyList<Module> modules)
    {
        Assert.IsNotNull(modules);
        Assert.IsTrue(modules.Count > moduleIndex, "Module index was out of range. Are you using this transformed module with the modules it was compiled with?");

        var module = modules[moduleIndex];

        return TransformedModule.GetFaceLayoutIndices(module, isFlipped, yawIndex);
    }

    private static FaceData<FaceLayoutIndex> GetYawedFaceIndex(Module.FaceIndices faceIndices, int yawIndex)
    {
        Assert.IsNotNull(faceIndices.sides);
        Assert.IsNotNull(faceIndices.top);
        Assert.IsNotNull(faceIndices.bottom);
        return new FaceData<FaceLayoutIndex>{
            forward = faceIndices.sides[GetYawedFaceIndex(0, yawIndex)],
            right = faceIndices.sides[GetYawedFaceIndex(1, yawIndex)],
            back = faceIndices.sides[GetYawedFaceIndex(2, yawIndex)],
            left = faceIndices.sides[GetYawedFaceIndex(3, yawIndex)],
            up = faceIndices.top[yawIndex],
            down = faceIndices.bottom[yawIndex],
        };
    }

    private static int GetYawedFaceIndex(int faceIndex, int yawIndex)
    {
        return (faceIndex + 4 - yawIndex) % 4;
    }

    public int GetHash()
    {
        // module index will be low almost all of the time
        if (moduleIndex > 0x0fff)
        {
            Debug.LogWarning($"Module index is larger than {0x0fff}. This will start causing hash collisions");
        }
        var hashSeed = moduleIndex
            + (isFlipped ? 1 : 0) << 12
            + yawIndex << 13;
        return Hash.Int(hashSeed);
    }

    /// <summary>
    /// Constructs a human-readable name for this TransformedModule. This is not necessarily unique.
    ///
    /// This string might change between versions of Wildtile.
    /// </summary>
    public string GetUserFriendlyName(IList<Module> modules)
    {
        var module = modules[moduleIndex];
        var moduleName = module.prefab?.name ?? "<no prefab>"; 
        return $"{moduleName} ({moduleIndex}){(isFlipped ? " flipped" : "")} yaw {90 * yawIndex}";
    }
}

}

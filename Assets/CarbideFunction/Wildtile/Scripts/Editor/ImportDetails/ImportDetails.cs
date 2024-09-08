using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// Information about the import, including any issues encountered.
/// </summary>
[Serializable]
internal class ImportDetails
{
    public string exceptionMessage = null;

    [Serializable]
    public class ModuleWithFacesOnACubeFace
    {
        public int moduleIndex = -1;
        public string cachedName = String.Empty;
        public int numberOfFacesOnCubeFace = 0;
    }

    [Serializable]
    public class HashCollision
    {
        public FaceLayoutHash collidingHash;

        public FaceLayoutHash oppositeHash0;
        public ModuleFaceIdentifier moduleFace0;
        public FaceLayoutHash oppositeHash1;
        public ModuleFaceIdentifier moduleFace1;
    }

    public List<ModuleWithFacesOnACubeFace> modulesWithFacesOnACubeFace = new List<ModuleWithFacesOnACubeFace>();

    public List<HashCollision> hashCollisions = new List<HashCollision>();

    [Serializable]
    public class SuperInsideCornerModule
    {
        public int moduleIndex;
        public string cachedName;

        public int firstSuperInsideOutsideCornerIndex;
    }

    public List<SuperInsideCornerModule> superInsideCornerModules = new List<SuperInsideCornerModule>();

    [Serializable]
    public class OutOfBoundsModule
    {
        public string cachedName;
    }

    public List<OutOfBoundsModule> outOfBoundsModules = new List<OutOfBoundsModule>();
}

}

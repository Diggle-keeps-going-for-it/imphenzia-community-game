using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

internal class ImportedModule
{
    public string name;
    public GameObject prefab;
    public ModuleMesh mesh;

    public struct FaceHashes
    {
        public FaceLayoutHash[] top;
        public FaceLayoutHash[] bottom;
        public FaceLayoutHash[] sides;
    }

    public FaceHashes faceHashes;
    public FaceHashes flippedFaceHashes;
}

}

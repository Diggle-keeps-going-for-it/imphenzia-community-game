using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class ReimportAllMenuItem
    {
        [MenuItem("Assets/Wildtile/Reimport all tilesets", false, 9701)]
        private static void Reimport()
        {
            ReimportAll.Reimport();
        }
    }
}

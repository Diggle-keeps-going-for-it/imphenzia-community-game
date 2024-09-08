using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class MultipleFaceLayoutHashToIndexMapperExtensions
    {
        internal static FaceLayoutIndex[] ConvertVerticalHashesToIndices(this FaceLayoutHashToIndexMapper mapper, FaceLayoutHash[] hashes)
        {
            return hashes.Select(hash => mapper.ConvertVerticalHash(hash)).ToArray();
        }

        internal static FaceLayoutIndex[] ConvertHorizontalHashesToIndices(this FaceLayoutHashToIndexMapper mapper, FaceLayoutHash[] hashes)
        {
            return hashes.Select(hash => mapper.ConvertHorizontalHash(hash)).ToArray();
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarbideFunction.Wildtile.Editor
{
    /// <summary>
    /// This class maps <see ref="FaceLayoutHash">FaceLayoutHashes</see> to <see ref="FaceLayoutIndex">FaceLayoutIndices</see> for a given tileset. It can be used as part of an import process to optimise many operations.
    ///
    /// For example, the propagator can check the matching face for a given module using a single array lookup (O(1)) instead of a binary search (O(log(n))).
    ///
    /// Another example is this allows slots to store "modules remaining satisfying a face in a direction" with just the number of modules. This can be easily looked up, again with O(1) cost, to update when a module is removed and to see if further propagation is required 
    /// </summary>
    internal class FaceLayoutHashToIndexMapper
    {
        private FaceLayoutHash[] horizontalHashes;
        private FaceLayoutHash[] verticalHashes;

        public struct MapperAndMatchingFaceIndices
        {
            public FaceLayoutHashToIndexMapper mapper;
            public FaceLayoutIndex[] horizontalMatchingFaces;
            public FaceLayoutIndex[] verticalMatchingFaces;
        }

        internal static MapperAndMatchingFaceIndices CreateMapper
        (
            IEnumerable<KeyValuePair<FaceLayoutHash, FaceLayoutHash>> horizontalMatchingFaces,
            IEnumerable<KeyValuePair<FaceLayoutHash, FaceLayoutHash>> verticalMatchingFaces
        )
        {
            var horizontalHashList = GetKeyHashesFromMatchingHashes(horizontalMatchingFaces);
            var verticalHashList = GetKeyHashesFromMatchingHashes(verticalMatchingFaces);
            
            var mapper = new FaceLayoutHashToIndexMapper{
                horizontalHashes = horizontalHashList,
                verticalHashes = verticalHashList,
            };

            return new MapperAndMatchingFaceIndices{
                mapper = mapper,
                horizontalMatchingFaces = horizontalMatchingFaces.Select(hashToMatchingHash => mapper.ConvertHorizontalHash(hashToMatchingHash.Value)).ToArray(),
                verticalMatchingFaces = verticalMatchingFaces.Select(hashToMatchingHash => mapper.ConvertVerticalHash(hashToMatchingHash.Value)).ToArray(),
            };
        }

        private static FaceLayoutHash[] GetKeyHashesFromMatchingHashes(IEnumerable<KeyValuePair<FaceLayoutHash, FaceLayoutHash>> matchingFaces)
        {
            return matchingFaces.Select(hashToMatchingHash => hashToMatchingHash.Key).ToArray();
        }

        private static FaceLayoutIndex ConvertHash(FaceLayoutHash hash, FaceLayoutHash[] orderedHashes)
        {
            var hashIndex = Array.FindIndex(orderedHashes, arrayHash => arrayHash.Equals(hash));
            if (hashIndex == -1)
            {
                throw new ArgumentException($"The hash {hash} is not in the ordered hashes");
            }
            return FaceLayoutIndex.FromRawInt((System.UInt32)hashIndex);
        }

        public FaceLayoutIndex ConvertHorizontalHash(FaceLayoutHash hash)
        {
            return ConvertHash(hash, horizontalHashes);
        }

        public FaceLayoutIndex ConvertVerticalHash(FaceLayoutHash hash)
        {
            return ConvertHash(hash, verticalHashes);
        }
    }
}

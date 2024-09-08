using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Assertions;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// A processed tileset stored in a Unity asset, ready for use in the Wildtile implementation of the wave function collapse algorithm.
///
/// &gt; [!WARNING]
/// &gt; This asset is not designed to be directly modified by users of Wildtile. Instead, use a <see cref="TilesetImporterAsset"/> and TilesetImporter to populate the Tileset asset.
/// &gt; Take special care if editing it directly.
/// </summary>
[CreateAssetMenu(fileName="New Tileset", menuName= MenuConstants.topMenuName+"Tileset", order=MenuConstants.orderBase+1)]
public class Tileset : ScriptableObject
{
    [SerializeField]
    internal List<Module> modules;
    /// <summary>
    /// Readonly access to the raw modules. These are not transformed and do not have their corner contents.
    /// </summary>
    public List<Module> Modules => modules;

    /// <summary>
    /// Contains the starting point for slots for a marching cube.
    ///
    /// &gt; [!WARNING]
    /// &gt; This class is not designed to be directly modified by users of Wildtile. Instead, use a <see cref="TilesetImporterAsset"/> and TilesetImporter to populate the <see cref="Tileset"/> asset.
    /// &gt; Take special care if editing it directly.
    /// </summary>
    [Serializable]
    internal struct CubeConfiguration
    {
        public List<TransformedModule> availableModules;
        public FaceData<int[]> startingSupportedFaceCounts;
    }

    // indexed as:
    //  --X = x
    //  -X- = y
    //  X-- = z
    // so e.g. 010b is the left upper close corner
    // and 011b is the left upper far corner
    [SerializeField]
    private CubeConfiguration[] marchingCubeLookup = CreateResetCubeLookup();
    internal CubeConfiguration[] MarchingCubeLookup => marchingCubeLookup;
    internal const string marchingCubeLookupName = nameof(marchingCubeLookup);

    [SerializeField]
    [FormerlySerializedAs("horizontalMatchingFaceIndices")]
    internal FaceLayoutIndex[] horizontalMatchingFaceLayoutIndices;

    [SerializeField]
    [FormerlySerializedAs("verticalMatchingFaceIndices")]
    internal FaceLayoutIndex[] verticalMatchingFaceLayoutIndices;

    [SerializeField]
    internal Vector3 tileDimensions = Vector3.one;
    /// <summary>
    /// Readonly access to the size of a single tile's bounding box in this tileset.
    /// All tiles have that same bounding box.
    ///
    /// To change the tileDimensions, change the tileDimensions in <see cref="TilesetImporterAsset"/> and reimport the tileset.
    /// </summary>
    public Vector3 TileDimensions => tileDimensions;

    [SerializeField]
    internal Postprocessing.Postprocessor postprocessor = null;
    /// <summary>
    /// Readonly access to the postprocessor.
    /// </summary>
    public Postprocessing.Postprocessor Postprocessor => postprocessor;

    internal static void CopyStartingFaceCountsToSlotFaceCounts(FaceData<int[]> startingFaceCounts, ref FaceData<SlotHalfLoop> slotHalfLoops)
    {
        foreach (var faceData in FaceDataSerialization.serializationFaces)
        {
            var face = faceData.face;
            Assert.IsNotNull(startingFaceCounts[face]);
            // if this slot is on the edge of a map then the half loops pointing into space will be null
            if (slotHalfLoops[face] != null)
            {
                slotHalfLoops[face].moduleCountsSupportingFaceLayouts = (int[])startingFaceCounts[face].Clone();
            }
        }
    }

    internal void ResetCubeLookup()
    {
        marchingCubeLookup = CreateResetCubeLookup();
    }

    private static CubeConfiguration[] CreateResetCubeLookup()
    {
        return Enumerable.Range(0,256).Select(i => new CubeConfiguration{availableModules = new List<TransformedModule>()}).ToArray();
    }
}

}

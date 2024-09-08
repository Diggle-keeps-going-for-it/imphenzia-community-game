using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class provides the implementation of the analysis of user's marching cubes, reporting any errors or warnings it finds.
/// These reports indicate to the user what is wrong and the user can make necessary steps to remedy them, or ignore them in the case of benign warnings.
///
/// The errors and warnings are split into three categories:
/// <list type="table">
/// <listheader>
///   <term>Error/Warning Name</term>
///   <description>Description</description>
/// </listheader>
/// <item>
/// <term>Structural errors</term>
/// <description>
/// These errors are unrecoverable, and will likely prevent both the rest of the analysis and the WFC algorithm from running. They include cases such as internal indices being out of range, or two modules having the same ID.
///
/// The CarbideFunction importer will not generate assets with these errors - if the asset was edited by hand then a reimport will fix them. If a custom importer generated the asset then the importer should be fixed.
/// </description>
/// </item>
///
/// <item>
///   <term>Content errors</term>
///   <description>
/// These errors will prevent modules from being available in the generated map. This means that the work put in to create and maintain them will never be seen by the player.
/// This includes cases such as a module's face not matching any other module faces in the asset, so it will never be picked.
///   </description>
/// </item>
///
/// <item>
///   <term>Marching cube warnings</term>
///   <description>
/// These warnings indicate that some voxel maps will not work if they include specific layouts of voxels. They are a normal and commonplace occurance when developing a tileset, and are even seen in production ready tilesets - for example, a city tileset will typically be built from the ground up, and might not include the modules for voxels overhanging one another.
///   </description>
/// </item>
/// </list>
/// </summary>
internal static class TilesetAnalyzer
{
    /// <summary>
    /// Describes a module that cannot be connected and indicates which face cannot be satisfied. Used by <see cref="TilesetAnalyzer.Report"/>.
    /// </summary>
    [Serializable]
    public class UnconnectableModule
    {
        /// <summary>
        /// The module name is cached to keep it available even if the user changes the report's marching cubes asset.
        /// </summary>
        [SerializeField]
        public string cachedName = "<Unknown>";
        [SerializeField]
        public Face face;

        public string GetUserFriendlyIdentifier()
        {
            return ConstructUnconnectableModuleName(cachedName, FaceDataSerialization.ToSerializationFace(face));
        }
    }

    /// <summary>
    /// Describes a marching cube asset's issues.
    /// </summary>
    [Serializable]
    public class Report
    {
        [SerializeField]
        public UnconnectableModule[] unconnectableModules;

        [SerializeField]
        public int[] emptyMarchingCubeBuckets;

        public bool IsValid => unconnectableModules != null && emptyMarchingCubeBuckets != null;
    }


    /// <summary>
    /// Analyzes the <paramref name="marchingCubes"/> using the <paramref name="searchParameters"/>, returning a report that can be cross-referenced with the passed in marching cubes or the search parameters to describe the problems.
    /// </summary>
    public static Report GenerateReport(Tileset marchingCubes, IEnumerable<SearchConfiguration> searchParameters)
    {
        return new Report{
            unconnectableModules = FindAllUnconnectableModules(marchingCubes),
            emptyMarchingCubeBuckets = FindAllEmptyMarchingCubeBucketIndices(marchingCubes, searchParameters)
        };
    }

    private static int[] FindAllEmptyMarchingCubeBucketIndices(Tileset marchingCubes, IEnumerable<SearchConfiguration> searchParameters)
    {
        Assert.IsNotNull(searchParameters);
        Assert.IsNotNull(searchParameters);
        return searchParameters
            .Select((searchConfiguration, index) => new {searchConfiguration, index})
            .Where(config => marchingCubes.MarchingCubeLookup[config.searchConfiguration.marchingCubeConfig].availableModules.Count == 0)
            .Select(config => config.index)
            .ToArray();
    }

    private static UnconnectableModule[] FindAllUnconnectableModules(Tileset marchingCubes)
    {
        return EnumerateAllUnconnectableModules(marchingCubes).ToArray();
    }

    private static IEnumerable<UnconnectableModule> EnumerateAllUnconnectableModules(Tileset marchingCubes)
    {
        foreach (var moduleAndContents in EnumerateModulesAndContents(marchingCubes))
        {
            foreach (var face in FaceDataSerialization.serializationFaces)
            {
                var faceIndex = moduleAndContents.module.GetFaceLayoutIndices(marchingCubes.modules)[face.face];
                var isVerticalFace = face.face == Face.Up || face.face == Face.Down;
                var oppositeIndex = (isVerticalFace ? marchingCubes.verticalMatchingFaceLayoutIndices : marchingCubes.horizontalMatchingFaceLayoutIndices)[faceIndex.Index];

                var oppositeFace = FaceDataSerialization.GetOppositeFace(face.face);
                var hasFoundConnectable = false;
                var numberOfModulesInspected = 0;
                foreach (var marchingCubesBucketIndex in FaceDataSerialization.EnumerateMatchingVertexContents(moduleAndContents.contents, face))
                {
                    var marchingCubesBucket = marchingCubes.MarchingCubeLookup[marchingCubesBucketIndex].availableModules;
                    foreach (var module in marchingCubesBucket)
                    {
                        numberOfModulesInspected ++;
                        var otherFaceIndex = module.GetFaceLayoutIndices(marchingCubes.modules)[oppositeFace];
                        if (oppositeIndex.Equals(otherFaceIndex))
                        {
                            hasFoundConnectable = true;
                            break;
                        }
                    }

                    if (hasFoundConnectable)
                    {
                        break;
                    }
                }

                if (!hasFoundConnectable)
                {
                    yield return new UnconnectableModule{
                        cachedName = GetTransformedModuleName(marchingCubes, moduleAndContents.module),
                        face = face.face,
                    };
                }
            }
        }
    }

    private static string GetTransformedModuleName(Tileset marchingCubes, TransformedModule transformedModule)
    {
        var module = marchingCubes.modules[transformedModule.moduleIndex];
        return module.name;
    }

    private static string ConstructUnconnectableModuleName(string moduleName, FaceDataSerialization.SerializationFace face)
    {
        return $"{moduleName} - {face.facePropertyName}";
    }

    private struct ModuleAndContents
    {
        public TransformedModule module;
        public int moduleIndex;
        public int contents;
    }

    private static IEnumerable<ModuleAndContents> EnumerateModulesAndContents(Tileset marchingCubes)
    {
        for (var marchingCubeIndex = 0; marchingCubeIndex < marchingCubes.MarchingCubeLookup.GetLength(0); ++marchingCubeIndex)
        {
            var cubeConfig = marchingCubes.MarchingCubeLookup[marchingCubeIndex];
            foreach (var transformedModule in cubeConfig.availableModules)
            {
                if (transformedModule.yawIndex == 0 && !transformedModule.isFlipped)
                {
                    yield return new ModuleAndContents{
                        module = transformedModule,
                        moduleIndex = transformedModule.moduleIndex,
                        contents = marchingCubeIndex
                    };
                }
            }
        }
    }
}

}

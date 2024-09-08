using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class ModulesSupportingFaceLayoutCounter
    {
        internal static void CalculateAndSaveCubeConfigurationStartingSupportedFaceCounts
        (
            SerializedProperty marchingCubeConfigs,
            int horizontalFaceCount,
            int verticalFaceCount,
            IReadOnlyList<Module> modules
        )
        {
            for (var cubeConfigIndex = 0; cubeConfigIndex < marchingCubeConfigs.arraySize; ++cubeConfigIndex)
            {
                var serializedCubeConfig = marchingCubeConfigs.GetArrayElementAtIndex(cubeConfigIndex);

                var startingFaceIndices = CountModulesSupportingFaceLayouts(serializedCubeConfig, horizontalFaceCount, verticalFaceCount, modules);
                WriteModuleCountsSupportingFaceLayoutsToProperty(startingFaceIndices, serializedCubeConfig);
            }
        }

        // internal to allow testing
        internal static FaceData<int[]> CountModulesSupportingFaceLayouts
        (
            SerializedProperty serializedCubeConfig,
            int horizontalFaceCount,
            int verticalFaceCount,
            IReadOnlyList<Module> modules
        )
        {
            var accumulatingModuleCountsSupportingFaceLayouts = FaceData<int[]>.Create(
                faceCount => new int[faceCount],
                horizontalFaceCount, horizontalFaceCount, horizontalFaceCount, horizontalFaceCount,
                verticalFaceCount, verticalFaceCount
            );

            var serializedTransformedModules = serializedCubeConfig.FindPropertyRelative(nameof(Tileset.CubeConfiguration.availableModules));

            for (var transformedModuleIndex = 0; transformedModuleIndex < serializedTransformedModules.arraySize; ++transformedModuleIndex)
            {
                var serializedTransformedModule = serializedTransformedModules.GetArrayElementAtIndex(transformedModuleIndex);
                var reconstructedTransformedModule = ReconstructTransformedModule(serializedTransformedModule);
                var faceIndices = reconstructedTransformedModule.GetFaceLayoutIndices(modules);

                foreach (var face in FaceDataSerialization.serializationFaces)
                {
                    var faceIndexRaw = faceIndices[face.face].Index;
                    accumulatingModuleCountsSupportingFaceLayouts[face.face][faceIndexRaw]++;
                }
            }

            return accumulatingModuleCountsSupportingFaceLayouts;
        }

        private static TransformedModule ReconstructTransformedModule(SerializedProperty serializedTransformedModule)
        {
            return new TransformedModule{
                moduleIndex = serializedTransformedModule.FindPropertyRelative(nameof(TransformedModule.moduleIndex)).intValue,
                isFlipped = serializedTransformedModule.FindPropertyRelative(nameof(TransformedModule.isFlipped)).boolValue,
                yawIndex = serializedTransformedModule.FindPropertyRelative(nameof(TransformedModule.yawIndex)).intValue,
                selectionWeight = serializedTransformedModule.FindPropertyRelative(nameof(TransformedModule.selectionWeight)).floatValue,
            };
        }

        private static void WriteModuleCountsSupportingFaceLayoutsToProperty(FaceData<int[]> moduleCountsSupportingFaceLayouts, SerializedProperty serializedCubeConfig)
        {
            var serializedStartingModuleCountsSupportingFaceLayouts = serializedCubeConfig.FindPropertyRelative(nameof(Tileset.CubeConfiguration.startingSupportedFaceCounts));

            foreach (var face in FaceDataSerialization.serializationFaces)
            {
                var serializedFaceData = serializedStartingModuleCountsSupportingFaceLayouts.FindPropertyRelative(face.facePropertyName);
                WriteArrayToSerializedProperty(moduleCountsSupportingFaceLayouts[face.face], serializedFaceData);
            }
        }

        private static void WriteArrayToSerializedProperty(int[] supportingModuleCounts, SerializedProperty serializedStartingModuleCountsSupportingFaceLayouts)
        {
            serializedStartingModuleCountsSupportingFaceLayouts.arraySize = supportingModuleCounts.Length;

            for (var i = 0; i < supportingModuleCounts.Length; ++i)
            {
                serializedStartingModuleCountsSupportingFaceLayouts.GetArrayElementAtIndex(i).intValue = supportingModuleCounts[i];
            }
        }
    }
}

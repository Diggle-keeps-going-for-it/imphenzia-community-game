using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CarbideFunction.Wildtile.Editor
{
    internal static class RenderPipelineDetector
    {
        public enum RenderPipeline
        {
            NoneOrUnrecognized,
            Universal,
            HighDefinition,
        }
        public static RenderPipeline GetProjectRenderPipeline()
        {
            if (IsProjectUsingUniversalRenderPipeline())
            {
                return RenderPipeline.Universal;
            }
            else if (IsProjectUsingHighDefinitionRenderPipeline())
            {
                return RenderPipeline.HighDefinition;
            }
            else
            {
                return RenderPipeline.NoneOrUnrecognized;
            }
        }

        public static bool IsProjectUsingUniversalRenderPipeline()
        {
            return IsProjectUsingPipelineWithClassNameContaining("universal");
        }

        public static bool IsProjectUsingHighDefinitionRenderPipeline()
        {
            return IsProjectUsingPipelineWithClassNameContaining("hdrenderpipeline");
        }

        public static string currentPipelineAssetClassName
        {
            get {
                var pipelineAsset = GraphicsSettings.currentRenderPipeline;
                if (pipelineAsset == null)
                {
                    return null;
                }
                else
                {
                    return pipelineAsset.GetType().Name;
                }
            }
        }

        private static bool IsProjectUsingPipelineWithClassNameContaining(string testName)
        {
            return currentPipelineAssetClassName?.ToLower()?.Contains(testName) ?? false;
        }
    }
}

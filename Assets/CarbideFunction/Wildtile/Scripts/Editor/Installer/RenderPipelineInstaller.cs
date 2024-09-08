using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    public class RenderPipelineInstaller : ScriptableSingleton<RenderPipelineInstaller>
    {
        public void InstallRenderPipelineSpecificPackages()
        {
            switch (RenderPipelineDetector.GetProjectRenderPipeline())
            {
                case RenderPipelineDetector.RenderPipeline.Universal:
                    OfferToInstallPackage("Universal Render Pipeline", universalUpgradePackage);
                    break;
                case RenderPipelineDetector.RenderPipeline.HighDefinition:
                    OfferToInstallPackage("High Definition Render Pipeline", highDefinitionUpgradePackage);
                    break;
                default:
                    break;
            }
        }

        [SerializeField]
        private UnityEngine.Object universalUpgradePackage;

        [SerializeField]
        private UnityEngine.Object highDefinitionUpgradePackage;

        private static void OfferToInstallPackage(string inferredRenderPipeline, UnityEngine.Object packageAsset)
        {
            if (packageAsset == null)
            {
                var errorMessage = $"Wanted to install Wildtile support for {inferredRenderPipeline} but the package was unavailable. The support packages are included in the Wildtile asset store files. Was it removed from the asset store import?";
                Debug.LogError(errorMessage);
                EditorUtility.DisplayDialog("Failed to Install Wildtile", errorMessage, "OK");
                throw new System.IO.FileNotFoundException(errorMessage);
            }

            var userResponse = AskUserIfTheyWantPackageInstalling(inferredRenderPipeline);
            switch (userResponse)
            {
                case UserAuthorizesInstall.ViewUpgradePackages:
                    SelectPackage(packageAsset);
                    break;
                case UserAuthorizesInstall.Install:
                    InstallPackage(packageAsset);
                    break;
                case UserAuthorizesInstall.Reject:
                    break;
            }
        }

        private enum UserAuthorizesInstall
        {
            Reject,
            ViewUpgradePackages,
            Install,
        }
        
        private static UserAuthorizesInstall AskUserIfTheyWantPackageInstalling(string inferredRenderPipeline)
        {
            var selectPackagePrompt = "Select Package";
            var responseIndex = EditorUtility.DisplayDialogComplex(
                "Install Render Pipeline-specific Wildtile Support?", $"Your project appears to use the {inferredRenderPipeline}. Wildtile needs to install a package of extra shaders and materials to match your render pipeline otherwise some tools won't work.\n\nThis installer can be rerun at any time from the Project Settings window.\n\nClicking \"{selectPackagePrompt}\" will allow you to view the available packages in your project window, then you can install the correct one by double clicking on it.",
                "Install",
                "Cancel",
                selectPackagePrompt
            );

            // https://docs.unity3d.com/ScriptReference/EditorUtility.DisplayDialogComplex.html
            // 0 = OK
            // 1 = Cancel
            // 2 = Alt
            switch (responseIndex)
            {
                case 0:
                    return UserAuthorizesInstall.Install;
                case 1:
                    return UserAuthorizesInstall.Reject;
                case 2:
                    return UserAuthorizesInstall.ViewUpgradePackages;
                default:
                    Debug.LogError($"Unrecognized dialog response code: {responseIndex}");
                    return UserAuthorizesInstall.Reject;
            }
        }

        private static void SelectPackage(UnityEngine.Object packageAsset)
        {
            // select package so the project window navigates to it
            Selection.activeObject = packageAsset;
        }

        private static void InstallPackage(UnityEngine.Object packageAsset)
        {
            var assetPath = AssetDatabase.GetAssetPath(packageAsset);
            AssetDatabase.ImportPackage(assetPath, interactive:false);
        }
    }
}

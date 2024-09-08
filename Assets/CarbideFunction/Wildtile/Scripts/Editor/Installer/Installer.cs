using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    [InitializeOnLoad]
    internal class Installer : ScriptableSingleton<Installer>
    {
        [Serializable]
        public class Data
        {
            [SerializeField]
            internal bool hasInstalled;

            public void OnInspectorGUI()
            {
                if (GUILayout.Button("Rerun Installer"))
                {
                    Installer.instance.Install();
                }

                using (var disabler = new EditorGUI.DisabledScope(true))
                {
                    var installTags = Installer.LoadInstallTags();

                    EditorGUILayout.Toggle(
                        new GUIContent(
                            "Is Installed",
                            "Has the install completed on any version of Wildtile?"
                        ),
                        Installer.HasInstalledAnyVersion(installTags)
                    );

                    EditorGUILayout.Toggle(
                        new GUIContent(
                            $"Is {CurrentVersionText} Installed",
                            "Has the install completed for this version of Wildtile? If the install has completed for any version of Wildtile but not for this version, please delete the Wildtile directory and reimport from the Asset Store."
                        ),
                        Installer.HasInstalledCurrentVersion(installTags)
                    );

                    EditorGUILayout.Toggle(
                        new GUIContent(
                            "Has Ever Installed",
                            "Has Wildtile ever been installed in this project? This flag is used to detect when to upgrade existing tilesets while installing Wildtile."
                        ),
                        hasInstalled
                    );
                }
            }
        }

        private static Data data => ProjectSettings.instance.installer;

        private static string CurrentVersionText => InstallTagConstant.tag;
        private const string tagFilePath = "Assets/CarbideFunction/Wildtile/install_tag.asset";
        // This file is a script that was available in all previous versions of Wildtile up to version 4.0.0
        // which is when Wildtile introduced the explicit installer tag file.
        private const string pre4ScriptFilePath = "Assets/CarbideFunction/Wildtile/Scripts/Editor/Utility/CustomDrawing.cs";

        static Installer()
        {
            EditorApplication.delayCall += () => instance.EnsureInstalled();
        }

        private class ProjectInstallTags
        {
            internal TextAsset installerTagFile;
            internal bool pre4ScriptFileExists;
        }

        private static ProjectInstallTags LoadInstallTags()
        {
            return new ProjectInstallTags{
                installerTagFile = TryLoadInstallerTagFile(),
                pre4ScriptFileExists = CheckPre4ScriptFile()
            };
        }

        private void EnsureInstalled()
        {
            var installTags = LoadInstallTags();

            if (HasInstalledAnyVersion(installTags))
            {
                if (!HasInstalledCurrentVersion(installTags))
                {
                    WarnToDeleteBeforeInstallingUpgrades();
                }
            }
            else
            {
                Install();
            }
        }

        private static TextAsset TryLoadInstallerTagFile()
        {
            return AssetDatabase.LoadAssetAtPath<TextAsset>(tagFilePath);
        }

        private static bool CheckPre4ScriptFile()
        {
            return AssetDatabase.LoadAssetAtPath<TextAsset>(pre4ScriptFilePath) != null;
        }

        private static void WriteInstallerTagFile()
        {
            var asset = new TextAsset(CurrentVersionText);
            AssetDatabase.CreateAsset(asset, tagFilePath);
        }

        private void WarnToDeleteBeforeInstallingUpgrades()
        {
            EditorUtility.DisplayDialog(
                "Upgrading Wildtile",
                "It appears you are upgrading Wildtile. To upgrade Wildtile safely, delete the Wildtile folder completely (Assets/CarbideFunction/Wildtile) and then install it again from the Asset Store.",
                "OK"
            );
        }

        private static bool HasInstalledAnyVersion(ProjectInstallTags installTags)
        {
            return installTags.installerTagFile != null || installTags.pre4ScriptFileExists;
        }

        private static bool HasInstalledCurrentVersion(ProjectInstallTags installTags)
        {
            if (installTags.pre4ScriptFileExists)
            {
                return false;
            }

            if (installTags.installerTagFile == null)
            {
                return false;
            }

            var installedVersionText = installTags.installerTagFile.text;
            return installedVersionText == CurrentVersionText;
        }

        private void Install()
        {
            InstallRenderPipelineSpecificPackages();

            if (IsUpgrading())
            {
                OfferToReimportAllTilesets();
            }

            SaveProjectHasInstalledWildtile();
        }

        private void OfferToReimportAllTilesets()
        {
            if (EditorUtility.DisplayDialog(
                "Upgrading Wildtile",
                "It appears you are upgrading Wildtile. The Tileset format has changed and all Tilesets require a reimport. Would you like to reimport all tilesets now?",
                "Yes",
                "No"
            ))
            {
                ReimportAllTilesets();
            }
        }

        private bool IsUpgrading()
        {
            return data.hasInstalled;
        }

        private void ReimportAllTilesets()
        {
            ReimportAll.Reimport();
        }

        private static void SaveProjectHasInstalledWildtile()
        {
            data.hasInstalled = true;
            ProjectSettings.instance.Save();

            WriteInstallerTagFile();
        }

        private void InstallRenderPipelineSpecificPackages()
        {
            RenderPipelineInstaller.instance.InstallRenderPipelineSpecificPackages();
        }
    }
}

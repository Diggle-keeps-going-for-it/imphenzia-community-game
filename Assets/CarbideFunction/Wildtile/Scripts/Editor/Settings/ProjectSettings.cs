using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
    [FilePath("ProjectSettings/Wildtile.asset", FilePathAttribute.Location.ProjectFolder)]
    [InitializeOnLoad]
    internal class ProjectSettings : ScriptableSingleton<ProjectSettings>
    {
        [SerializeField]
        public Installer.Data installer = new Installer.Data();

        public void Save()
        {
            base.Save(true);
        }
    }
}

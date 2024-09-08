using System.IO;
using UnityEditor;

namespace CarbideFunction.Wildtile.Editor
{
/// <summary>
/// Methods for interacting with the filesystem's folders/directories.
/// </summary>
public static class Folder
{
    /// <summary>
    /// Safely create the folder. If it already exists, silently do nothing.
    /// </summary>
    public static string EnsureFolderExists(string root, string folderName)
    {
        var fullFolderPath = Path.Join(root, folderName);
        if (!AssetDatabase.IsValidFolder(fullFolderPath))
        {
            AssetDatabase.CreateFolder(root, folderName);
        }
        return fullFolderPath;
    }
}
}

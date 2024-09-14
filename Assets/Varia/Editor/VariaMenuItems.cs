using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Varia
{
    public static class VariaMenuItems
    {

        [MenuItem("Tools/Varia/Randomize then Freeze", true)]
        public static bool FreezeValidate()
        {
            return Selection.gameObjects.Length > 0;
        }

        private const string FreezeUndoGroup = "Varia Freeze";

        [MenuItem("Tools/Varia/Randomize then Freeze")]
        public static void Freeze()
        {
            try
            {
                VariaContext.current.recordUndo = true;
                foreach (var go in Selection.gameObjects)
                {
                    if (go.TryGetComponent<VariaPreviewer>(out var previewer))
                    {
                        // Freeze previewer by disabling the previewer,
                        // setting the objects as savable
                        Undo.RegisterFullObjectHierarchyUndo(previewer, FreezeUndoGroup);
                        previewer.enabled = false;
                        foreach(var child in previewer.transform.Cast<Transform>())
                        {
                            Undo.RegisterFullObjectHierarchyUndo(child, FreezeUndoGroup);
                            child.hideFlags = HideFlags.None;
                        }
                    }
                    else
                    {
                        // Freeze other objects by applying all their Varia behaviours
                        if (PrefabUtility.IsAnyPrefabInstanceRoot(go))
                            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.UserAction);
                        Undo.RegisterFullObjectHierarchyUndo(go, FreezeUndoGroup);
                        VariaContext.current.Freeze(go);
                    }
                }
            }
            finally
            {
                VariaContext.current.recordUndo = false;
            }
        }
    }
}

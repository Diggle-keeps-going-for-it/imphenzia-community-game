using UnityEditor;


namespace Varia
{
    [CustomEditor(typeof(VariaKeep))]
    [CanEditMultipleObjects]
    public class VariaKeepEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if ((target as VariaBehaviour).conditionList.conditions.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no conditions set up, so this component will not do anything at all.", MessageType.Warning);
            }
        }
    }
}
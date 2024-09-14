using UnityEditor;


namespace Varia
{
    [CustomEditor(typeof(VariaDestroy))]
    [CanEditMultipleObjects]
    public class VariaDestroyEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if((target as VariaBehaviour).conditionList.conditions.Count == 0)
            {
                EditorGUILayout.HelpBox("There are no conditions set up, so this component will be immediately destroyed after creation.", MessageType.Warning);
            }
        }
    }
}
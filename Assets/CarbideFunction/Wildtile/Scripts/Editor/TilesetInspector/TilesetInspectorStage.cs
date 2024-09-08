using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.SceneManagement;
using CarbideFunction.Wildtile;

namespace CarbideFunction.Wildtile.Editor
{

/// <summary>
/// This class acts as a <see href="https://docs.unity3d.com/ScriptReference/SceneManagement.Stage.html">Unity Stage</see> that manages instances of Wildtile module prefabs. It is created and decorated by <see cref="TilesetInspector"/>.
/// </summary>
internal class TilesetInspectorStage : PreviewSceneStage
{
    [SerializeField]
    private GameObject currentModule = null;
    [SerializeField]
    private GameObject connectedModule = null;

    protected override GUIContent CreateHeaderContent()
    {
        return new GUIContent("Tileset Inspector");
    }

    protected override bool OnOpenStage()
    {
        base.OnOpenStage();
        return true;
    }

    protected override void OnFirstTimeOpenStageInSceneView(SceneView view)
    {
        base.OnFirstTimeOpenStageInSceneView(view);

        view.sceneLighting = false;
        var cameraPosition = new Vector3(-0.5f, 5f, -3f);
        view.camera.transform.position = cameraPosition;
        view.camera.transform.rotation = Quaternion.LookRotation(-cameraPosition, Vector3.up);
    }

    public void ModuleSelected(GameObject prefab, ModuleMesh mesh, Vector3 tileDimensions, int yawIndex, bool isFlipped)
    {
        InstantiateModule(ref currentModule, prefab, mesh, yawIndex, isFlipped, tileDimensions, Vector3.zero);
    }

    public void ConnectedModuleSelected(Vector3 delta, GameObject prefab, ModuleMesh mesh, Vector3 tileDimensions, int yawIndex, bool isFlipped, bool isValid)
    {
        InstantiateModule(ref connectedModule, prefab, mesh, yawIndex, isFlipped, tileDimensions, delta);
    }

    private void InstantiateModule(ref GameObject instance, GameObject prefab, ModuleMesh moduleMesh, int yawIndex, bool isFlipped, Vector3 dimensions, Vector3 offset)
    {
        if (scene.IsValid())
        {
            ClearModule(ref instance);

            instance = new GameObject("Instance root");
            SceneManager.MoveGameObjectToScene(instance, scene);

            if (prefab != null)
            {
                var prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                prefabInstance.transform.SetParent(instance.transform);
                ApplyModuleTransform(prefabInstance.transform, yawIndex, isFlipped, offset);
            }

            if (moduleMesh != null)
            {
                var halfDimensions = dimensions * 0.5f;
                var unityMesh = ModuleMeshInstantiator.InstantiateMeshAndCageWarp(moduleMesh, false,
                    new Vector3(-halfDimensions.x, -halfDimensions.y, -halfDimensions.z),
                    new Vector3( halfDimensions.x, -halfDimensions.y, -halfDimensions.z),
                    new Vector3(-halfDimensions.x,  halfDimensions.y, -halfDimensions.z),
                    new Vector3( halfDimensions.x,  halfDimensions.y, -halfDimensions.z),
                    new Vector3(-halfDimensions.x, -halfDimensions.y,  halfDimensions.z),
                    new Vector3( halfDimensions.x, -halfDimensions.y,  halfDimensions.z),
                    new Vector3(-halfDimensions.x,  halfDimensions.y,  halfDimensions.z),
                    new Vector3( halfDimensions.x,  halfDimensions.y,  halfDimensions.z),
                    NormalWarper.identityWarper
                );
                var meshObject = new GameObject("Instance mesh");
                SceneManager.MoveGameObjectToScene(meshObject, scene);
                ModuleMeshInstantiator.AddMeshToObject(meshObject, moduleMesh, unityMesh);

                meshObject.transform.SetParent(instance.transform);
                ApplyModuleTransform(meshObject.transform, yawIndex, isFlipped, offset);
            }

            ApplyPreviewObjectSettings(instance);
        }
    }

    private void ApplyModuleTransform(Transform target, int yawIndex, bool isFlipped, Vector3 offset)
    {
        target.localPosition = offset;
        target.localRotation = Quaternion.AngleAxis(90f * yawIndex, Vector3.up);
        target.localScale = new Vector3(isFlipped ? -1f : 1f, 1f, 1f);
    }

    private void ApplyPreviewObjectSettings(GameObject target)
    {
        if (!UserSettings.instance.showTilesetInspectorStageObjects)
        {
            foreach (var transform in target.GetComponentsInChildren<Transform>())
            {
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
                SceneVisibilityManager.instance.DisablePicking(transform.gameObject, true);
            }
        }
    }

    private void ClearModule(ref GameObject toBeCleared)
    {
        if (toBeCleared != null)
        {
            GameObject.DestroyImmediate(toBeCleared);
            toBeCleared = null;
        }
    }
}

}

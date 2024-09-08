using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

using CarbideFunction.Wildtile.Postprocessing;

namespace CarbideFunction.Wildtile.PlacerPostprocessing
{

/// <summary>
/// Simple postprocessor that spawns the module prefabs with the specified transform.
/// </summary>
public class PlacerPostprocessor : Postprocessor
{
    public override IEnumerable<int> Postprocess(GameObject root, PostprocessableMap map, Vector3 tileDimensions)
    {
        var inverseTileDimensions = new Vector3(1f / tileDimensions.x, 1f / tileDimensions.y, 1f / tileDimensions.z);

        var meshes = new Dictionary<ModuleMesh, Mesh>();
        foreach (var slot in map.slots)
        {
            var positionWarper = new VertexWarper(
                slot.v000, slot.v001,
                slot.v010, slot.v011,
                slot.v100, slot.v101,
                slot.v110, slot.v111
            );

            if (slot.prefab != null)
            {
                var instance = GameObject.Instantiate(slot.prefab, root.transform);
                WarpDirectChildrenPositions(positionWarper, slot.normalWarper, instance.transform, tileDimensions, inverseTileDimensions);
            }

            if (slot.mesh != null && !slot.mesh.IsEmpty())
            {
                var mesh = CreateAndWarpUnityMeshForModuleMesh(slot, slot.mesh);
                var instance = new GameObject(slot.moduleName);
                instance.transform.SetParent(root.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                ModuleMeshInstantiator.AddMeshToObject(instance, slot.mesh, mesh);
            }

            yield return 0;
        }
    }

    private Mesh CreateAndWarpUnityMeshForModuleMesh(PostprocessableMap.Slot slot, ModuleMesh moduleMesh)
    {
        var newlyCreatedMesh = ModuleMeshInstantiator.InstantiateMeshAndCageWarp(moduleMesh,
            slot.flipIndices,
            slot.v000, slot.v001, slot.v010, slot.v011,
            slot.v100, slot.v101, slot.v110, slot.v111,
            slot.normalWarper
        );
        return newlyCreatedMesh;
    }

    private void WarpDirectChildrenPositions(VertexWarper positionWarper, NormalWarper normalWarper, Transform rootTransform, Vector3 tileDimensions, Vector3 inverseTileDimensions)
    {
        foreach (Transform child in rootTransform)
        {
            // make sure to fully read input values before writing to any of the values
            // i.e. do not write the local position immediately after getting it
            var unitCubePosition = Vector3.Scale(child.localPosition, inverseTileDimensions);
            var mapSpacePosition = positionWarper.WarpPosition(unitCubePosition);

            var unitCubeForward = Vector3.Scale(child.localRotation * Vector3.forward, tileDimensions);
            var unitCubeUp = Vector3.Scale(child.localRotation * Vector3.up, tileDimensions);
            var forwardFacing = normalWarper.WarpNormal(unitCubePosition, unitCubeForward);
            var upFacing = normalWarper.WarpNormal(unitCubePosition, unitCubeUp);

            child.localPosition = mapSpacePosition;
            child.localRotation = Quaternion.LookRotation(forwardFacing, upFacing);;
        }
    }
}

}

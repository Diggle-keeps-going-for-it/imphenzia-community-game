using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

using CarbideFunction.Wildtile;
using CarbideFunction.Wildtile.Postprocessing;

public class LevelEditorPostProcessor : Postprocessor
{
    public override IEnumerable<int> Postprocess(GameObject root, PostprocessableMap map, Vector3 tileDimensions)
    {
        var meshes = new Dictionary<ModuleMesh, Mesh>();
        var placedModulesComponent = root.AddComponent<PlacedModulesRecord>();
        var placedModules = new PlacedModulesRecord.PlacedModule[map.slots.Length];
        var voxelToSlotMap = new Dictionary<int, List<int>>();
        for (var i = 0; i < map.slots.Length; ++i)
        {
            var slot = map.slots[i];
            var newPlacedModule = new PlacedModulesRecord.PlacedModule();
            placedModules[i] = newPlacedModule;
            newPlacedModule.sourceVoxels = slot.sourceVoxels;
            newPlacedModule.instance = CreateInstance(slot, root.transform, meshes);

            if (newPlacedModule.instance != null)
            {
                newPlacedModule.instance.SetActive(false);
            }

            AddVoxelDependentSlots(voxelToSlotMap, i, slot.sourceVoxels.Indices);

            yield return 0;
        }

        for (var i = 0; i < map.slots.Length; ++i)
        {
            var instance = placedModules[i].instance;

            if (instance != null)
            {
                instance.SetActive(true);
            }
        }

        placedModulesComponent.voxelIndexToSlotIndices = new Dictionary<int, int[]>(voxelToSlotMap.Select(mapping => new KeyValuePair<int, int[]>(mapping.Key, mapping.Value.ToArray())));
        placedModulesComponent.placedModules = placedModules;
    }

    private void AddVoxelDependentSlots(Dictionary<int, List<int>> voxelIndexToSlotIndexMap, int slotIndex, IEnumerable<int> sourceVoxelIndices)
    {
        foreach (var sourceVoxelIndex in sourceVoxelIndices)
        {
            if (sourceVoxelIndex != IVoxelGrid.outOfBoundsVoxelIndex)
            {
                var dependentSlots = EnsureAndGetVoxelToSlotMap(voxelIndexToSlotIndexMap, sourceVoxelIndex);
                Assert.IsFalse(dependentSlots.Contains(slotIndex), $"Voxel {sourceVoxelIndex} has already got {slotIndex} as a dependent");
                dependentSlots.Add(slotIndex);
            }
        }
    }

    private List<int> EnsureAndGetVoxelToSlotMap(Dictionary<int, List<int>> voxelToSlotMap, int voxelIndex)
    {
        if (voxelToSlotMap.ContainsKey(voxelIndex))
        {
            return voxelToSlotMap[voxelIndex];
        }
        else
        {
            var newList = new List<int>();
            voxelToSlotMap.Add(voxelIndex, newList);
            return newList;
        }
    }

    private GameObject CreateInstance(PostprocessableMap.Slot slot, Transform root, Dictionary<ModuleMesh, Mesh> meshes)
    {
        if (slot.mesh == null && slot.prefab == null)
        {
            return null;
        }

        var instance = new GameObject(slot.moduleName);
        instance.transform.SetParent(root.transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        var positionWarper = new VertexWarper(
            slot.v000, slot.v001,
            slot.v010, slot.v011,
            slot.v100, slot.v101,
            slot.v110, slot.v111
        );

        if (slot.mesh != null && !slot.mesh.IsEmpty())
        {
            var mesh = CreateAndWarpUnityMeshForModuleMesh(slot, slot.mesh);
            var meshInstance = new GameObject(slot.moduleName);
            meshInstance.transform.SetParent(instance.transform);
            meshInstance.transform.localPosition = Vector3.zero;
            meshInstance.transform.localRotation = Quaternion.identity;
            ModuleMeshInstantiator.AddMeshToObject(meshInstance, slot.mesh, mesh);

            AddMeshCollider(meshInstance);
        }

        if (slot.prefab != null)
        {
            var prefabInstance = GameObject.Instantiate(slot.prefab, instance.transform);
            WarpDirectChildrenPositions(positionWarper, slot.normalWarper, prefabInstance.transform);
        }

        return instance;
    }

    private Mesh FindOrCreateUnityMeshForModuleMesh(ModuleMesh moduleMesh, Dictionary<ModuleMesh, Mesh> meshes)
    {
        if (meshes.TryGetValue(moduleMesh, out var mesh))
        {
            return mesh;
        }
        else
        {
            var newlyCreatedMesh = ModuleMeshInstantiator.InstantiateMesh(moduleMesh, false);
            meshes.Add(moduleMesh, newlyCreatedMesh);
            return newlyCreatedMesh;
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


    private void AddMeshCollider(GameObject spawnedModule)
    {
        var meshFilter = spawnedModule.GetComponentInChildren<MeshFilter>();
        if (meshFilter != null)
        {
            var mesh = meshFilter.sharedMesh;
            var meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }
    }

    private void WarpDirectChildrenPositions(VertexWarper positionWarper, NormalWarper normalWarper, Transform rootTransform)
    {
        foreach (Transform child in rootTransform)
        {
            // make sure to fully read input values before writing to any of the values
            // i.e. do not write the local position immediately after getting it
            var mapSpacePosition = positionWarper.WarpPosition(child.localPosition);
            var sourceRightFacing = child.localRotation * Vector3.right;
            var rightFacing = normalWarper.WarpNormal(child.localPosition, sourceRightFacing);
            var forwardFacing = Vector3.Cross(rightFacing, Vector3.up);

            child.localPosition = mapSpacePosition;
            child.localRotation = Quaternion.LookRotation(forwardFacing);;
        }
    }
}

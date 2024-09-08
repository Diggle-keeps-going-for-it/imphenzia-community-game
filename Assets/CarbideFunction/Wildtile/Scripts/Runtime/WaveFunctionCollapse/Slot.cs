using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// A slot is a cube that should be filled by a <see cref="TransformedModule"/>.
///
/// At the start of a collapse, a Slot might have the choice of many TransformedModules. Collapsing the map will attempt to remove modules from slots until one is left in every slot.
///
/// If the collapse is successful, each Slot will have one TransformedModule in <see cref="availableModules"/>.
///
/// If the collapse is unsuccessful then some Slots will become wildcards. Wildcards can connect to any TransformedModule in neighbouring slots.
///
/// You can also set a slot to be a wildcard before starting a collapse.
/// </summary>
public class Slot
{
    internal int RemoveAllOtherModulesAndDoNotPropagate(TransformedModule keepModule, IReadOnlyList<Module> modules)
    {
        Assert.IsTrue(availableModules.Contains(keepModule));

        var previousCount = availableModules.Count;

        availableModules.Clear();
        availableModules.Add(keepModule);

        SetSlotSupportedFaceLayoutsFromSingleModule(keepModule, modules);

        return previousCount - 1;
    }

    private void PropagateModuleRemoval(TransformedModule transformedModule, IReadOnlyList<Module> modules, IList<Slot> appendToDirtySlots)
    {
        RemoveModuleSupportsFromSlotHalfLoops(transformedModule, modules, appendToDirtySlots);
    }

    internal void RestoreModules
    (
        IEnumerable<TransformedModule> transformedModules,
        IReadOnlyList<Module> modules
    )
    {
        availableModules.AddRange(transformedModules);

        foreach (var transformedModule in transformedModules)
        {
            AddModuleSupportsForSlotHalfLoops(transformedModule, modules);
        }
    }

    private void RestoreModule
    (
        TransformedModule transformedModule,
        IReadOnlyList<Module> modules
    )
    {
        Assert.IsFalse(availableModules.Contains(transformedModule));
        availableModules.Add(transformedModule);
        AddModuleSupportsForSlotHalfLoops(transformedModule, modules);
    }

    internal bool IsWildcard()
    {
        return isWildcard;
    }

    private void RemoveModuleSupportsFromSlotHalfLoops
    (
        TransformedModule transformedModule,
        IReadOnlyList<Module> modules,
        IList<Slot> dirtySlots
    )
    {
        var moduleFaceLayouts = transformedModule.GetFaceLayoutIndices(modules);
        foreach (var faceDefinition in FaceDataSerialization.serializationFaces)
        {
            var face = faceDefinition.face;
            var slotHalfLoop = halfLoops[face];
            if (slotHalfLoop != null)
            {
                var layoutIndexAlongFace = moduleFaceLayouts[face].Index;
                Assert.IsNotNull(slotHalfLoop.moduleCountsSupportingFaceLayouts);
                Assert.IsTrue(slotHalfLoop.moduleCountsSupportingFaceLayouts.Length > layoutIndexAlongFace);
                --slotHalfLoop.moduleCountsSupportingFaceLayouts[layoutIndexAlongFace];

                Assert.IsTrue(slotHalfLoop.moduleCountsSupportingFaceLayouts[layoutIndexAlongFace] >= 0);

                if (slotHalfLoop.moduleCountsSupportingFaceLayouts[layoutIndexAlongFace] == 0)
                {
                    MarkSlotAsDirty(slotHalfLoop.targetSlot, dirtySlots);
                }
            }
        }
    }

    private void AddModuleSupportsForSlotHalfLoops
    (
        TransformedModule transformedModule,
        IReadOnlyList<Module> modules
    )
    {
        var moduleFaceLayouts = transformedModule.GetFaceLayoutIndices(modules);
        foreach (var faceDefinition in FaceDataSerialization.serializationFaces)
        {
            var face = faceDefinition.face;
            var slotHalfLoop = halfLoops[face];
            if (slotHalfLoop != null)
            {
                var layoutIndexAlongFace = moduleFaceLayouts[face].Index;
                Assert.IsNotNull(slotHalfLoop.moduleCountsSupportingFaceLayouts);
                Assert.IsTrue(slotHalfLoop.moduleCountsSupportingFaceLayouts.Length > layoutIndexAlongFace);
                ++slotHalfLoop.moduleCountsSupportingFaceLayouts[layoutIndexAlongFace];
            }
        }
    }

    private static void MarkSlotAsDirty(Slot slot, IList<Slot> dirtySlots)
    {
        if (!slot.IsWildcard() && !dirtySlots.Contains(slot))
        {
            dirtySlots.Add(slot);
        }
    }

    /// <summary>
    /// Wildcard slots can connect to any neighbour TransformedModules.
    ///
    /// When collapsing, TransformedModules are normally removed if the neighbour slots contain no modules that share their face layouts. If the neighbour slot is a wildcard then this check is skipped.
    /// </summary>
    public bool isWildcard = false;

    private List<TransformedModule> availableModules;
    /// <summary>
    /// Readonly access to the available modules for this slot. Will change throughout a collapse.
    /// </summary>
    public IReadOnlyList<TransformedModule> AvailableModules => availableModules;

    internal void SetAvailableModules
    (
        IEnumerable<TransformedModule> newAvailableModules,
        FaceData<int[]> moduleCountSupportingFaceLayouts
    )
    {
        availableModules = new List<TransformedModule>(newAvailableModules);

        foreach (var faceDefinition in FaceDataSerialization.serializationFaces)
        {
            var face = faceDefinition.face;
            var slotHalfLoop = halfLoops[face];
            if (slotHalfLoop != null)
            {
                slotHalfLoop.moduleCountsSupportingFaceLayouts = (int[])moduleCountSupportingFaceLayouts[face].Clone();
            }
        }
    }

    internal int RemoveInvalidModules(IReadOnlyList<Module> modules, FaceLayoutIndex[] horizontalFaceToRequiredFace, FaceLayoutIndex[] verticalFaceToRequiredFace, List<TransformedModule> removedModules, IList<Slot> dirtySlots)
    {
        removingInvalidModulesPerfMarker.Begin();
        // swap remove algorithm
        var endIndex = AvailableModules.Count;
        var checkIndex = 0;
        while (checkIndex < endIndex)
        {
            if (!IsTransformedModuleValidInSlot( availableModules[checkIndex], modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace))
            {
                removedModules.Add(availableModules[checkIndex]);
                PropagateModuleRemoval(availableModules[checkIndex], modules, dirtySlots);
                --endIndex;
                while (checkIndex < endIndex)
                {
                    if (IsTransformedModuleValidInSlot( availableModules[endIndex], modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace))
                    {
                        availableModules[checkIndex] = availableModules[endIndex];
                        break;
                    }
                    else
                    {
                        PropagateModuleRemoval(availableModules[endIndex], modules, dirtySlots);
                        removedModules.Add(availableModules[endIndex]);
                    }

                    --endIndex;
                }
            }

            ++checkIndex;
        }

        var numberOfRemovedModules = availableModules.Count - endIndex;

        availableModules.RemoveRange(endIndex, numberOfRemovedModules);
        Assert.AreEqual(availableModules.Count, endIndex);
        Assert.IsTrue(!availableModules.Any(module => removedModules.Contains(module)));
        removingInvalidModulesPerfMarker.End();
        return numberOfRemovedModules;
    }

    internal bool IsTransformedModuleValidInSlot(TransformedModule module, IReadOnlyList<Module> modules, FaceLayoutIndex[] horizontalFaceToRequiredFace, FaceLayoutIndex[] verticalFaceToRequiredFace)
    {
        testingIfModuleIsValidPerfMarker.Begin();
        var localModuleFaceIndexes = module.GetFaceLayoutIndices(modules);

        // search for problems, return false on first one found (if any)
        foreach (var serializationFace in FaceDataSerialization.serializationFaces)
        {
            var face = serializationFace.face;
            var directionSelectedFaceToFaceMappings = IsFaceVertical(face) ? verticalFaceToRequiredFace : horizontalFaceToRequiredFace; 
            findFaceIndexPerfMarker.Begin();
            var requiredFaceIndex = directionSelectedFaceToFaceMappings[localModuleFaceIndexes[face].Index].Index;
            findFaceIndexPerfMarker.End();

            var slotHalfLoop = halfLoops[face];

            if (slotHalfLoop != null)
            {
                var targetSlot = slotHalfLoop.targetSlot;

                if (targetSlot.IsWildcard())
                {
                    // Target slot is a wildcard, passing
                }
                else
                {
                    searchingForFaceInNeighbourSlotPerfMarker.Begin();
                    var facingHalfLoop = targetSlot.halfLoops[slotHalfLoop.facingFaceOnTarget];
                    Assert.IsNotNull(facingHalfLoop);
                    Assert.IsNotNull(facingHalfLoop.moduleCountsSupportingFaceLayouts);
                    Assert.IsTrue(facingHalfLoop.moduleCountsSupportingFaceLayouts.Length > requiredFaceIndex);
                    if (facingHalfLoop.moduleCountsSupportingFaceLayouts[requiredFaceIndex] == 0)
                    {
                        searchingForFaceInNeighbourSlotPerfMarker.End();
                        testingIfModuleIsValidPerfMarker.End();
                        return false;
                    }
                    searchingForFaceInNeighbourSlotPerfMarker.End();
                }
            }
            else
            {
                // Target slot is outside the grid, passing
            }
        }

        // didn't find any problems
        testingIfModuleIsValidPerfMarker.End();
        return true;
    }

    private static bool IsFaceVertical(Face face)
    {
        switch (face)
        {
            case (Face.Up):
            case (Face.Down):
                return true;
            default:
                return false;
        }
    }

    private void SetSlotSupportedFaceLayoutsFromSingleModule(TransformedModule transformedModule, IReadOnlyList<Module> modules)
    {
        var supportedFaces = transformedModule.GetFaceLayoutIndices(modules);
        foreach (var faceDefinition in FaceDataSerialization.serializationFaces)
        {
            var face = faceDefinition.face;
            var halfLoop = halfLoops[face];
            if (halfLoop != null)
            {
                Array.Fill(halfLoop.moduleCountsSupportingFaceLayouts, 0);
                var supportedFaceLayoutInThisDirection = supportedFaces[face];
                halfLoop.moduleCountsSupportingFaceLayouts[supportedFaceLayoutInThisDirection.Index] = 1;
            }
        }
    }

    internal FaceData<SlotHalfLoop> halfLoops;

    internal Vector3 v000;
    internal Vector3 normalX000;
    internal Vector3 normalY000;
    internal Vector3 normalZ000;

    internal Vector3 v001;
    internal Vector3 normalX001;
    internal Vector3 normalY001;
    internal Vector3 normalZ001;

    internal Vector3 v010;
    internal Vector3 normalX010;
    internal Vector3 normalY010;
    internal Vector3 normalZ010;

    internal Vector3 v011;
    internal Vector3 normalX011;
    internal Vector3 normalY011;
    internal Vector3 normalZ011;

    internal Vector3 v100;
    internal Vector3 normalX100;
    internal Vector3 normalY100;
    internal Vector3 normalZ100;

    internal Vector3 v101;
    internal Vector3 normalX101;
    internal Vector3 normalY101;
    internal Vector3 normalZ101;

    internal Vector3 v110;
    internal Vector3 normalX110;
    internal Vector3 normalY110;
    internal Vector3 normalZ110;

    internal Vector3 v111;
    internal Vector3 normalX111;
    internal Vector3 normalY111;
    internal Vector3 normalZ111;

    /// <summary>
    /// Readonly access to the bottom left back corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V000 => v000;
    /// <summary>
    /// Readonly access to the bottom left back corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX000 => normalX000;
    /// <summary>
    /// Readonly access to the bottom left back corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY000 => normalY000;
    /// <summary>
    /// Readonly access to the bottom left back corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ000 => normalZ000;

    /// <summary>
    /// Readonly access to the bottom right back corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V001 => v001;
    /// <summary>
    /// Readonly access to the bottom right back corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX001 => normalX001;
    /// <summary>
    /// Readonly access to the bottom right back corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY001 => normalY001;
    /// <summary>
    /// Readonly access to the bottom right back corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ001 => normalZ001;

    /// <summary>
    /// Readonly access to the top left back corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V010 => v010;
    /// <summary>
    /// Readonly access to the top left back corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX010 => normalX010;
    /// <summary>
    /// Readonly access to the top left back corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY010 => normalY010;
    /// <summary>
    /// Readonly access to the top left back corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ010 => normalZ010;

    /// <summary>
    /// Readonly access to the top right back corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V011 => v011;
    /// <summary>
    /// Readonly access to the top right back corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX011 => normalX011;
    /// <summary>
    /// Readonly access to the top right back corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY011 => normalY011;
    /// <summary>
    /// Readonly access to the top right back corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ011 => normalZ011;

    /// <summary>
    /// Readonly access to the bottom left front corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V100 => v100;
    /// <summary>
    /// Readonly access to the bottom left front corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX100 => normalX100;
    /// <summary>
    /// Readonly access to the bottom left front corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY100 => normalY100;
    /// <summary>
    /// Readonly access to the bottom left front corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ100 => normalZ100;

    /// <summary>
    /// Readonly access to the bottom right front corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V101 => v101;
    /// <summary>
    /// Readonly access to the bottom right front corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX101 => normalX101;
    /// <summary>
    /// Readonly access to the bottom right front corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY101 => normalY101;
    /// <summary>
    /// Readonly access to the bottom right front corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ101 => normalZ101;

    /// <summary>
    /// Readonly access to the top left front corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V110 => v110;
    /// <summary>
    /// Readonly access to the top left front corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX110 => normalX110;
    /// <summary>
    /// Readonly access to the top left front corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY110 => normalY110;
    /// <summary>
    /// Readonly access to the top left front corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ110 => normalZ110;

    /// <summary>
    /// Readonly access to the top right front corner position for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 V111 => v111;
    /// <summary>
    /// Readonly access to the top right front corner x normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalX111 => normalX111;
    /// <summary>
    /// Readonly access to the top right front corner y normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalY111 => normalY111;
    /// <summary>
    /// Readonly access to the top right front corner z normal for this slot. Remains static throughout a collapse.
    /// </summary>
    public Vector3 NormalZ111 => normalZ111;

    /// <summary>
    /// Readonly access to the centroid for this slot. Deprecated.
    /// </summary>
    public Vector3 Position => (v000 + v001 + v010 + v011 + v100 + v101 + v110 + v111) / 8f;

    internal int contentsIndex;

    /// <summary>
    /// Stores the voxel indices that define this slot's corner contents.
    /// </summary>
    public struct SourceVoxels
    {
        public int voxel000Index;
        public int voxel001Index;
        public int voxel010Index;
        public int voxel011Index;
        public int voxel100Index;
        public int voxel101Index;
        public int voxel110Index;
        public int voxel111Index;

        /// <summary>
        /// Simple constructor
        /// </summary>
        public SourceVoxels
        (
            int voxel000Index,
            int voxel001Index,
            int voxel010Index,
            int voxel011Index,
            int voxel100Index,
            int voxel101Index,
            int voxel110Index,
            int voxel111Index
        )
        {
            this.voxel000Index = voxel000Index;
            this.voxel001Index = voxel001Index;
            this.voxel010Index = voxel010Index;
            this.voxel011Index = voxel011Index;
            this.voxel100Index = voxel100Index;
            this.voxel101Index = voxel101Index;
            this.voxel110Index = voxel110Index;
            this.voxel111Index = voxel111Index;
        }

        /// <summary>
        /// Easy access to all source voxel indices. This is intended to be used to construct the map of voxels to their dependent slots.
        /// </summary>
        public IEnumerable<int> Indices => new []{
            voxel000Index,
            voxel001Index,
            voxel010Index,
            voxel011Index,
            voxel100Index,
            voxel101Index,
            voxel110Index,
            voxel111Index,
        };
    }
    internal SourceVoxels sourceVoxels;

    private static readonly ProfilerMarker testingIfModuleIsValidPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Slot.TestIfModuleIsValid");
    private static readonly ProfilerMarker removingInvalidModulesPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Slot.RemovingInvalidModules");
    private static readonly ProfilerMarker findFaceIndexPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Slot.FindFace");
    private static readonly ProfilerMarker searchingForFaceInNeighbourSlotPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Slot.SearchForFaceInNeighbourModule");
}

}

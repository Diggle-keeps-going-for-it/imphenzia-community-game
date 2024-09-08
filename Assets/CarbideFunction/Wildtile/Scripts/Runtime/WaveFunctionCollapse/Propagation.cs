using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This class contains methods that propagate the adjacency constraints of modules around connected Slots.
///
/// It is used in wave function collapse after a single module has been chosen for a slot. It removes module choices from other slots that are incompatible with the selected module.
/// </summary>
internal static class Propagation
{
    /// <summary>
    /// When attempting to propagate the algorithm may reach an unsolvable state known as a contradiction.
    /// The methods in Propagation that can fail will return this enum.
    /// </summary>
    public enum SinglePropagationResult
    {
        Success_NotAffected,
        Success_Changed,
        Failed_Contradiction,
    }

    /// <summary>
    /// Tests and removes all invalid modules for a single slot and adds neighbour slots to the pending list of slots to visit.
    /// </summary>
    /// <param name="dirtySlot">The slot to test and modify.</param>
    /// <param name="modules">The list of modules for this tileset</param>
    /// <param name="horizontalFaceToRequiredFace">A map from module face layout indices to the face layout index required on the touching face of the adjacent module. This list is for horizontally opposite faces</param>
    /// <param name="verticalFaceToRequiredFace">A map from module face layout indices to the face layout index required on the touching face of the adjacent module. This list is for vertically opposite faces</param>
    /// <param name="removedModules">PropagateOne will add any removed modules to this list</param>
    /// <param name="dirtySlots">If PropagateOne removes the last supporting module for a face index in a direction, it will add the adjacent slot in that direction to the dirty slots list. The algorithm can then pop from this list and call PropagateOne on that slot, until no more slots are added.</param>
    /// <param name="contradictionReporter">If all modules are removed for this slot, the state is unsolveable. This is known as a contradiction. If this happens during this PropagateOne() call, <paramref name="contradictionReporter"/> will be called with <paramref name="dirtySlot"/>.</param>
    public static SinglePropagationResult PropagateOne(Slot dirtySlot, IReadOnlyList<Module> modules, FaceLayoutIndex[] horizontalFaceToRequiredFace, FaceLayoutIndex[] verticalFaceToRequiredFace, List<TransformedModule> removedModules, IList<Slot> dirtySlots, PropagationReporting.ReportContradictionSlot contradictionReporter = null)
    {
        propagateOnePerfMarker.Begin();

        var numberOfRemovedElements = dirtySlot.RemoveInvalidModules(modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace, removedModules, dirtySlots);
        if (dirtySlot.AvailableModules.Count == 0)
        {
            contradictionReporter?.Invoke(dirtySlot);
            propagateOnePerfMarker.End();
            return SinglePropagationResult.Failed_Contradiction;
        }

        propagateOnePerfMarker.End();
        return numberOfRemovedElements > 0
            ? SinglePropagationResult.Success_Changed
            : SinglePropagationResult.Success_NotAffected;
    }

    public static IEnumerable<CoroutineStatus> PropagateCoroutine(List<Slot> dirtySlots, IReadOnlyList<Module> modules, FaceLayoutIndex[] horizontalFaceToRequiredFace, FaceLayoutIndex[] verticalFaceToRequiredFace, PropagationReporting.ReportPropagationResult resultReporter, List<KeyValuePair<Slot, List<TransformedModule>>> removedModules, PropagationReporting.ReportContradictionSlot contradictionReporter = null)
    {
        postCollapsePropagatePerfMarker.Begin();
        while (dirtySlots.Count > 0)
        {
            var slot = dirtySlots.Last();
            dirtySlots.RemoveAt(dirtySlots.Count-1);

            removingModulesFromSelectCollapsedSlotPerfMarker.Begin();
            List<TransformedModule> removedModulesForThisSlot = null;
            foreach (var removedModulesRecord in removedModules)
            {
                if (removedModulesRecord.Key == slot)
                {
                    removedModulesForThisSlot = removedModulesRecord.Value;
                    break;
                }
            }
            if (removedModulesForThisSlot == null)
            {
                removedModulesForThisSlot = new List<TransformedModule>();
                removedModules.Add(new KeyValuePair<Slot, List<TransformedModule>>(slot, removedModulesForThisSlot));
            }
            Assert.IsNotNull(removedModulesForThisSlot);
            removingModulesFromSelectCollapsedSlotPerfMarker.End();

            postCollapsePropagatePerfMarker.End();
            yield return CoroutineStatus.Propagating;
            postCollapsePropagatePerfMarker.Begin();

            var propagationResult = Propagation.PropagateOne(slot, modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace, removedModulesForThisSlot, dirtySlots, contradictionReporter);

            switch (propagationResult)
            {
                case SinglePropagationResult.Success_NotAffected:
                {
                    break;
                }
                case SinglePropagationResult.Success_Changed:
                {
                    break;
                }
                case SinglePropagationResult.Failed_Contradiction:
                {
                    postPropagateReportResultPerfMarker.Begin();
                    resultReporter(PropagationResult.Contradiction);
                    postPropagateReportResultPerfMarker.End();
                    postCollapsePropagatePerfMarker.End();
                    yield break;
                }
                default:
                {
                    postCollapsePropagatePerfMarker.End();
                    Assert.IsTrue(false, $"Unknown propagation result {propagationResult}");
                    yield break;
                }
            }
        }
        postPropagateReportResultPerfMarker.Begin();
        resultReporter(PropagationResult.Success);
        postPropagateReportResultPerfMarker.End();
        postCollapsePropagatePerfMarker.End();
    }

    public delegate void DoWithSlot(Slot slot);

    public static void DoWithNeighbourSlots(Slot slot, DoWithSlot doWithNeighbour)
    {
        foreach (var faceDefinition in FaceDataSerialization.serializationFaces)
        {
            var face = faceDefinition.face;
            var halfLoop = slot.halfLoops[face];
            if (halfLoop != null)
            {
                doWithNeighbour(halfLoop.targetSlot);
            }
        }
    }

    private static readonly ProfilerMarker postCollapsePropagatePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Propagate");
    private static readonly ProfilerMarker addingNeighboursToDirtyPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Propagate.AddNeighboursToDirtySlots");
    private static readonly ProfilerMarker removingModulesFromSelectCollapsedSlotPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Propagate.RemoveModulesFromSelectCollapsedSlot");
    private static readonly ProfilerMarker propagateOnePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Propagate.PropagateOne");
    private static readonly ProfilerMarker postPropagateReportResultPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.Propagate.ReportResult");
}

}

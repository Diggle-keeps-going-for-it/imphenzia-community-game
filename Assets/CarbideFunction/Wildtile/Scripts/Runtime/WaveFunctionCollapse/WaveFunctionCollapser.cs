using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.Assertions;
using Unity.Profiling;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This object manages a run of the [wave function collapse algorithm](https://www.youtube.com/watch?v=2SuvO4Gi7uY) applied to 3D autotiling.
/// </summary>
public class WaveFunctionCollapser
{
    /// <summary>
    /// Create a wave function collapser instance. This is designed to be used with the fields from a <see cref="Tileset"/> asset.
    /// </summary>
    public WaveFunctionCollapser(
        IReadOnlyList<Module> modules,
        FaceLayoutIndex[] horizontalFaceToRequiredFace,
        FaceLayoutIndex[] verticalFaceToRequiredFace,
        int seed
    )
    {
        this.modules = modules;
        this.horizontalFaceToRequiredFace = horizontalFaceToRequiredFace;
        this.verticalFaceToRequiredFace = verticalFaceToRequiredFace;
        this.xorMask = SeedToXorMask(seed);
    }

    private IReadOnlyList<Module> modules;
    private FaceLayoutIndex[] horizontalFaceToRequiredFace;
    private FaceLayoutIndex[] verticalFaceToRequiredFace;
    private int xorMask;

    /// <summary>
    /// Start running a coroutine of the algorithm.
    /// </summary>
    /// <returns>
    /// A coroutine that will collapse the slots in <paramref name="operatingGrid"/>.
    /// </returns>
    public IEnumerable<CoroutineStatus> PropagateAll(SlotGrid operatingGrid, PropagationReporting.ReportPropagationResult resultReporter, PropagationReporting.ReportContradictionSlot reportContradictions, List<KeyValuePair<Slot, List<TransformedModule>>> removedModules)
    {
        foreach (var yieldable in Propagation.PropagateCoroutine(operatingGrid.SlotData.Where(slot => !slot.isWildcard).ToList(), modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace, resultReporter, removedModules, reportContradictions))
        {
            yield return yieldable;
        }
    }

    /// <summary>
    /// Starts a coroutine that calculates if the <paramref name="operatingGrid"/> is collapsible from its original state and emits the result to <paramref name="resultReporter"/>
    ///
    /// Always rewinds the slot grid back to the state it was when the coroutine was started. This function reads wildcard status, but does not change it.
    /// </summary>
    /// <param name="operatingGrid">The slot grid to operate on. During the coroutine, the slots' modules may change - these can be read (e.g. for animating the collapse) but writing to the slots during the coroutine may cause it to crash.</param>
    /// <param name="resultReporter">Once per coroutine this delegate will be called with the result of the calculation.</param>
    public IEnumerable<CoroutineStatus> PropagateAllAndCollapseAndRewind(SlotGrid operatingGrid, ReportCollapseResult resultReporter)
    {
        var removedModulesInPropagation = new List<KeyValuePair<Slot, List<TransformedModule>>>();

        PropagationResult? propagationResult = null;
        foreach (var yieldable in Propagation.PropagateCoroutine(operatingGrid.SlotData.Where(slot => !slot.isWildcard).ToList(), modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace, result => propagationResult = result, removedModulesInPropagation))
        {
            yield return yieldable;
        }

        Assert.IsTrue(propagationResult.HasValue);
        if (propagationResult.Value == PropagationResult.Success)
        {
            foreach (var yieldResult in Collapse(operatingGrid, resultReporter, rewindAfterCollapse:true))
            {
                yield return yieldResult;
            }
        }
        else
        {
            resultReporter.Invoke(CollapseResult.UnrecoverableContradiction);
        }

        foreach (var removedModulesSlot in removedModulesInPropagation)
        {
            removedModulesSlot.Key.RestoreModules(removedModulesSlot.Value, modules);
        }
    }

    /// <summary>
    /// If the algorithm successfully collapses it will report a <see cref="Success"/> value.
    /// If it exhausts all options over the <see cref="SlotGrid"/> and <see cref="Tileset"/>, the algorithm will report a <see cref="UnrecoverableContradiction"/> value.
    /// </summary>
    public enum CollapseResult
    {
        Success,
        UnrecoverableContradiction,
    }
    /// <summary>
    /// This delegate is used to report the final state of a collapse request.
    /// 
    /// The collapse request cannot directly return the value because coroutines yield-return many values and it is impossible to know if any given yield-return is the last one until after the coroutine completes.
    /// The collapser uses a delegate to make handling the result clear and readable.
    /// </summary>
    public delegate void ReportCollapseResult(CollapseResult result);


    /// <summary>
    /// Starts a coroutine that force-collapses the <paramref name="operatingGrid"/>, making slots wildcards where necessary.
    /// 
    /// This algorithm safely adds wildcards where slots have no available modules.
    /// 
    /// It attempts to reduce the number of wildcards in the grid, but it is not guaranteed to give the fewest possible wildcards for every grid.
    /// </summary>
    /// <param name="operatingGrid">The grid of slots that should be collapsed. At the end of the coroutine, all non-wildcard slots will have one module remaining. During the coroutine, the slots may lose and gain modules and their wildness may change - these can be read (e.g. for animating the collapse) but writing to the slots during the coroutine may cause it to crash.</param>
    public IEnumerable<CoroutineStatus> InitialPropagateAndCollapseWithWildcardRetreatAndAdvance
    (
        SlotGrid operatingGrid
    )
    {
        var wildcards = new List<Slot>();
        Assert.IsNotNull(operatingGrid.SlotData);
        foreach (var slot in operatingGrid.SlotData)
        {
            Assert.IsNotNull(slot);
            if (slot.AvailableModules.Count == 0)
            {
                slot.isWildcard = true;
            }

            // slot might have already been a wildcard, separate from previous logic
            if (slot.isWildcard)
            {
                wildcards.Add(slot);
            }
        }

        PropagationResult propagationResult = PropagationResult.Contradiction;
        var initialRemovedSlotModules = new List<KeyValuePair<Slot, List<TransformedModule>>>();
        var hasReportedCollapseFailure = false;
        while (propagationResult == PropagationResult.Contradiction)
        {
            var contradictingSlots = new HashSet<Slot>();

            foreach (var propagationYield in PropagateAll(operatingGrid, result => propagationResult = result, contradictingSlot => contradictingSlots.Add(contradictingSlot), initialRemovedSlotModules))
            {
                yield return propagationYield;
            }

            if (propagationResult == PropagationResult.Contradiction)
            {
                if (!hasReportedCollapseFailure)
                {
                    yield return CoroutineStatus.PerfectCollapseFailed;
                    hasReportedCollapseFailure = true;
                }

                foreach (var slot in contradictingSlots)
                {
                    slot.isWildcard = true;
                    wildcards.Add(slot);
                }
            }
        }

        // restore modules to slots, but keep them as wildcards
        // these modules will be required later for the wildcard reduction algorithm
        foreach (var removedSlotModules in initialRemovedSlotModules)
        {
            removedSlotModules.Key.RestoreModules(removedSlotModules.Value, modules);
        }

        initialRemovedSlotModules = null;

        foreach (var yieldResult in CollapseWithWildcardRetreatAndAdvance(operatingGrid, wildcards, hasReportedCollapseFailure))
        {
            yield return yieldResult;
        }
    }

    /// <summary>
    /// Starts a coroutine that force-collapses the <paramref name="operatingGrid"/>, making slots wildcards where necessary.
    /// 
    /// This algorithm assumes that all non-wildcard slots have at least one available module.
    /// 
    /// It attempts to reduce the number of wildcards in the grid, but it is not guaranteed to give the fewest possible wildcards for every grid.
    /// </summary>
    /// <param name="operatingGrid">The grid of slots that should be collapsed. At the end of the coroutine, all non-wildcard slots will have one module remaining. During the coroutine, the slots may lose and gain modules and their wildness may change - these can be read (e.g. for animating the collapse) but writing to the slots during the coroutine may cause it to crash.</param>
    private IEnumerable<CoroutineStatus> CollapseWithWildcardRetreatAndAdvance
    (
        SlotGrid operatingGrid,
        List<Slot> wildcardSlots,
        bool hasAlreadyReportedCollapseFailure
    )
    {
        var lastCollapseResult = CollapseResult.UnrecoverableContradiction;
        var initialPropagationRemovedModules = new List<KeyValuePair<Slot, List<TransformedModule>>>();

        var hasReportedCollapseFailure = hasAlreadyReportedCollapseFailure;

        while (lastCollapseResult == CollapseResult.UnrecoverableContradiction)
        {
            var propagationResult = PropagationResult.Contradiction;
            foreach (var propagationYield in PropagateAll(
                operatingGrid,
                result => propagationResult = result,
                contradictingSlot => Debug.Assert(false, "Found contradiction in initial propagation after more wildcards were added."),
                initialPropagationRemovedModules)
            )
            {
                yield return propagationYield;
            }

            var contradictingSlots = new HashSet<Slot>();

            foreach (var yieldResult in Collapse(operatingGrid, result => lastCollapseResult = result, contradictingSlot => contradictingSlots.Add(contradictingSlot), rewindAfterCollapse:true))
            {
                yield return yieldResult;
            }

            // if it failed, make all the contradicting slots wildcards
            if (lastCollapseResult == CollapseResult.UnrecoverableContradiction)
            {
                if (!hasReportedCollapseFailure)
                {
                    yield return CoroutineStatus.PerfectCollapseFailed;
                    hasReportedCollapseFailure = true;
                }

                foreach (var slot in contradictingSlots)
                {
                    slot.isWildcard = true;
                }
                wildcardSlots.AddRange(contradictingSlots);
            }

            // restore the modules to their original states, as more options may have opened up with the new wildcards
            foreach (var removedSlotModules in initialPropagationRemovedModules)
            {
                removedSlotModules.Key.RestoreModules(removedSlotModules.Value, modules);
            }
            initialPropagationRemovedModules.Clear();
        }

        if (hasReportedCollapseFailure)
        {
            yield return CoroutineStatus.FoundCollapsableState;
        }
        else
        {
            yield return CoroutineStatus.PerfectCollapseSucceeded;
        }

        List<List<Slot>> minimalWildcards = null;
        foreach (var yieldResult in WildcardSlotReducer.StartRemovingWildcards(wildcardSlots, reportCollapsible => StartCollapsingAndReportIsCollapsible(operatingGrid, reportCollapsible), result => minimalWildcards = result))
        {
            yield return yieldResult;
        }

        if (hasReportedCollapseFailure)
        {
            yield return CoroutineStatus.RemovedRedundantWildcards;
        }

        // finally, actually collapse with one of the collapsible maps with the fewest wildcards
        Assert.AreNotEqual(minimalWildcards.Count, 0);
        var selectedMinimalWildcards = minimalWildcards.Skip(1).Aggregate(minimalWildcards.First(), (shortest, next) => shortest.Count < next.Count ? shortest : next);
        foreach (var wildcardSlot in wildcardSlots)
        {
            if (!selectedMinimalWildcards.Contains(wildcardSlot))
            {
                wildcardSlot.isWildcard = false;
            }
        }

        foreach (var yieldResult in Collapse(operatingGrid, result => lastCollapseResult = result, null, rewindAfterCollapse:false))
        {
            yield return yieldResult;
        }
    }

    /// <summary>
    /// Start a coroutine that will attempt to collapse a SlotGrid and report whether it's collapsible or not.
    /// </summary>
    internal IEnumerable<CoroutineStatus> StartCollapsingAndReportIsCollapsible(SlotGrid slotGrid, WildcardSlotReducer.ReportCollapsible reportCollapsible)
    {
        // collapse grid with rewind
        var collapsibleResult = WaveFunctionCollapser.CollapseResult.UnrecoverableContradiction;

        foreach (var yieldResult in PropagateAllAndCollapseAndRewind(slotGrid, result => collapsibleResult = result))
        {
            yield return yieldResult;
        }

        reportCollapsible(collapsibleResult == WaveFunctionCollapser.CollapseResult.Success);
    }

    /// <summary>
    /// Starts a coroutine that collapses the slot grid.
    ///
    /// The coroutine will call <paramref name="resultReporter"/> by the end of the operation. It will call with <see cref="CollapseResult.Success"/> if the algorithm successfully collapsed the <paramref name="operatingGrid"/> to have one module in all non-wildcard slots, otherwise it will call with <see cref="CollapseResult.UnrecoverableContradiction"/>.
    /// </summary>
    /// <param name="operatingGrid">The grid of slots that should be collapsed. At the end of the coroutine, all non-wildcard slots will have one module remaining if the grid was collapsible and if <paramref name="rewindAfterCollapse"/> was false, otherwise it will return to its state when the coroutine started. During the coroutine, the slots may lose and gain modules - these can be read (e.g. for animating the collapse) but writing to the slots during the coroutine may cause it to crash.
    ///
    /// This coroutine will not affect the slots' wildness. </param>
    /// <param name="resultReporter">A delegate that will be called once during the coroutine, reporting if the algorithm successfully collapsed all non-wildcard slots to a single available module.</param>
    /// <param name="contradictionReporter">A delegate that will be called an unknown number of times during the coroutine, reporting any contradictions that cause the algorithm to rewind.</param>
    /// <param name="rewindAfterCollapse">If true, guarantees that the <paramref name="operatingGrid"/> will be returned to its original state even if the collapse was successful.</param>
    public IEnumerable<CoroutineStatus> Collapse(SlotGrid operatingGrid, ReportCollapseResult resultReporter, PropagationReporting.ReportContradictionSlot contradictionReporter = null, bool rewindAfterCollapse = false)
    {
        prepareCollapsePerfMarker.Begin();

        var firstStep = CreateCollapseStep(operatingGrid);

        if (firstStep.HasValue)
        {
            var steps = new Stack<CollapseAndPropagateStep>(new [] {firstStep.Value});
            prepareCollapsePerfMarker.End();

            while (true)
            {
                StepResult? stepResult = null;
                foreach (var yieldable in TryCollapseNextChoiceAndPropagateStep(steps, operationStepResult => stepResult = operationStepResult, contradictionReporter))
                {
                    yield return yieldable;
                }
                Assert.IsTrue(stepResult.HasValue, $"Completed execution of a step coroutine but it didn't populate '{nameof(stepResult)}'");

                postCollapseContinuationChoicePerfMarker.Begin();
                switch (stepResult.Value)
                {
                    case StepResult.Pass:
                    {
                        var nextStep = CreateCollapseStep(operatingGrid);
                        if (nextStep.HasValue)
                        {
                            steps.Push(nextStep.Value);
                        }
                        else
                        {
                            // fully collapsed
                            postCollapseContinuationChoicePerfMarker.End();
                            resultReporter(CollapseResult.Success);

                            if (rewindAfterCollapse)
                            {
                                while (steps.Count > 0)
                                {
                                    foreach (var yieldable in RewindStep(steps))
                                    {
                                        postCollapseContinuationChoicePerfMarker.End();
                                        yield return yieldable;
                                        postCollapseContinuationChoicePerfMarker.Begin();
                                    }
                                    steps.Pop();
                                }
                            }

                            yield break;
                        }
                        break;
                    }
                    case StepResult.Fail:
                    {
                        foreach (var yieldable in RewindStep(steps))
                        {
                            postCollapseContinuationChoicePerfMarker.End();
                            yield return yieldable;
                            postCollapseContinuationChoicePerfMarker.Begin();
                        }

                        break;
                    }
                    case StepResult.OutOfOptions:
                    {
                        steps.Pop();
                        if (steps.Count() == 0)
                        {
                            // collapse failed
                            postCollapseContinuationChoicePerfMarker.End();
                            resultReporter(CollapseResult.UnrecoverableContradiction);
                            yield break;
                        }
                        else
                        {
                            foreach (var yieldable in RewindStep(steps))
                            {
                                postCollapseContinuationChoicePerfMarker.End();
                                yield return yieldable;
                                postCollapseContinuationChoicePerfMarker.Begin();
                            }
                        }

                        break;
                    }
                    default:
                    {
                        postCollapseContinuationChoicePerfMarker.End();
                        Assert.IsTrue(false, $"Unknown step result: {stepResult.Value}");
                        throw new InvalidOperationException($"Unknown step result: {stepResult.Value}");
                    }
                }

                postCollapseContinuationChoicePerfMarker.End();
                yield return CoroutineStatus.CompletedCellCollapse;
            }
        }
        else
        {
            prepareCollapsePerfMarker.End();
            resultReporter(CollapseResult.Success);
            // already collapsed
            yield break;
        }
    }

    private CollapseAndPropagateStep? CreateCollapseStep(SlotGrid operatingGrid)
    {
        createNextStepPerfMarker.Begin();

        var lowestEntropy = int.MaxValue;

        var lowestCoordinateHash = uint.MaxValue;
        Slot chosenSlot = null;

        findNextSlotsPerfMarker.Begin();
        foreach (var slot in operatingGrid.SlotData)
        {
            if (!slot.isWildcard && slot.AvailableModules.Count > 1)
            {
                var slotEntropy = CalculateSlotEntropy(slot);
                if (slotEntropy < lowestEntropy)
                {
                    lowestEntropy = slotEntropy;
                    lowestCoordinateHash = CalculateCoordinateHash(slot, xorMask);
                    chosenSlot = slot;
                }
                else if (slotEntropy == lowestEntropy)
                {
                    var thisSlotCoordinateHash = CalculateCoordinateHash(slot, xorMask);

                    // Use less-equal here because it's possible that slots hash to int.MaxValue.
                    // This slightly favours later slots, but it's still deterministic in that favour.
                    if (thisSlotCoordinateHash <= lowestCoordinateHash)
                    {
                        lowestCoordinateHash = thisSlotCoordinateHash;
                        chosenSlot = slot;
                    }
                }
            }
        }
        findNextSlotsPerfMarker.End();

        CollapseAndPropagateStep? result = null;

        if (chosenSlot != null)
        {
            result = new CollapseAndPropagateStep{
                slot = chosenSlot,
                orderedAvailableModules = CreateOrderedAvailableModules(chosenSlot, lowestCoordinateHash, xorMask).GetEnumerator(),
                modulesRemovedThisStep = new List<KeyValuePair<Slot, List<TransformedModule>>>()
            };
        }

        createNextStepPerfMarker.End();
        return result;
    }

    private static int CalculateSlotEntropy(Slot slot)
    {
        calculateSlotChoicePriorityPerfMarker.Begin();
        // TODO
        calculateSlotChoicePriorityPerfMarker.End();
        return slot.AvailableModules.Count;
    }

    private static uint CalculateCoordinateHash(Slot slot, int xorMask)
    {
        calculateSlotHashPerfMarker.Begin();
        var result = Hash.QuantizedVector(slot.Position, 1) ^ xorMask;
        calculateSlotHashPerfMarker.End();
        return (uint)result;
    }

    private static IEnumerable<TransformedModule> CreateOrderedAvailableModules(Slot slot, uint precalculatedCoordinateHash, int xorMask)
    {
        createOrderedModulesPerfMarker.Begin();
        Assert.AreEqual(precalculatedCoordinateHash, CalculateCoordinateHash(slot, xorMask), "Supplied precalculated hash doesn't match the manual hash. This will silently fail in release builds.");
        var result = slot.AvailableModules.OrderByDescending(module => ((uint)module.GetHash() ^ precalculatedCoordinateHash) * module.selectionWeight).ToArray();
        createOrderedModulesPerfMarker.End();
        return result;
    }

    private enum StepResult
    {
        Pass,
        Fail,
        OutOfOptions,
    }
    private delegate void ReportCollapseStepResult(StepResult result);
    private IEnumerable<CoroutineStatus> TryCollapseNextChoiceAndPropagateStep(Stack<CollapseAndPropagateStep> steps, ReportCollapseStepResult resultReporter, PropagationReporting.ReportContradictionSlot contradictionReporter)
    {
        var step = steps.First();
        if (!step.orderedAvailableModules.MoveNext())
        {
            resultReporter(StepResult.OutOfOptions);
            yield break;
        }

        slotCollapsePerfMarker.Begin();

        var selectedModule = step.orderedAvailableModules.Current;
        Assert.IsTrue(step.slot.AvailableModules.Contains(selectedModule), "Selected a module that wasn't in the available modules");
        var priorNumberOfModules = step.slot.AvailableModules.Count();

        var removedModulesFromCollapse = new List<TransformedModule>(priorNumberOfModules - 1);
        foreach (var previouslyAvailableModule in step.slot.AvailableModules)
        {
            if (previouslyAvailableModule != selectedModule)
            {
                removedModulesFromCollapse.Add(previouslyAvailableModule);
            }
        }

        var removedModuleCount = step.slot.RemoveAllOtherModulesAndDoNotPropagate(selectedModule, modules);
        step.modulesRemovedThisStep.Add(new KeyValuePair<Slot, List<TransformedModule>>(step.slot, removedModulesFromCollapse));

        Assert.AreEqual(priorNumberOfModules, removedModuleCount + 1);

        var neighbours = new List<Slot>(6);
        Propagation.DoWithNeighbourSlots(step.slot, neighbourSlot =>
        {
            if (!neighbourSlot.isWildcard)
            {
                neighbours.Add(neighbourSlot);
            }
        });
        PropagationResult? propagationResult = null;
        foreach (var yieldable in Propagation.PropagateCoroutine(neighbours, modules, horizontalFaceToRequiredFace, verticalFaceToRequiredFace, operationResult => propagationResult = operationResult, step.modulesRemovedThisStep, contradictionReporter))
        {
            slotCollapsePerfMarker.End();
            yield return yieldable;
            slotCollapsePerfMarker.Begin();
        }

        Assert.IsTrue(propagationResult.HasValue);
        resultReporter(propagationResult.Value == PropagationResult.Success ? StepResult.Pass : StepResult.Fail);
        slotCollapsePerfMarker.End();
    }

    private IEnumerable<CoroutineStatus> RewindStep(Stack<CollapseAndPropagateStep> steps)
    {
        var step = steps.First();

        rewindPerfMarker.Begin();

        foreach (var removedModules in step.modulesRemovedThisStep)
        {
            removedModules.Key.RestoreModules(removedModules.Value, modules);
        }
        step.modulesRemovedThisStep.Clear();

        rewindPerfMarker.End();
        yield break;
    }

    private static int SeedToXorMask(int seed)
    {
        return Hash.Int(seed);
    }

    private struct CollapseAndPropagateStep
    {
        public Slot slot;
        public IEnumerator<TransformedModule> orderedAvailableModules;

        public List<KeyValuePair<Slot, List<TransformedModule>>> modulesRemovedThisStep;
    }

    private static readonly ProfilerMarker prepareCollapsePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.PrepareCollapse");
    private static readonly ProfilerMarker slotCollapsePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.CollapseSlot");
    private static readonly ProfilerMarker createNextStepPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.CreateNextStep");
    private static readonly ProfilerMarker postCollapseContinuationChoicePerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.ContinuationChoice");
    private static readonly ProfilerMarker rewindPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.RewindCollapse");
    private static readonly ProfilerMarker calculateSlotChoicePriorityPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.CalculateSlotChoicePriority");
    private static readonly ProfilerMarker calculateSlotHashPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.CalculateSlotHash");
    private static readonly ProfilerMarker findNextSlotsPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.FindNextSlots");
    private static readonly ProfilerMarker createOrderedModulesPerfMarker = new ProfilerMarker(ProfilerCategory.Scripts, "WaveFunctionCollapser.CreateOrderedModules");

}

}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// Manages an algorithm that finds the fewest number of wildcard slots that can still be collapsed by the wave function collapser.
///
/// Each time the collapser algorithm finds unrecoverable contradictions it will add many wildcards, normally many more than are required. This can be repeated until the grid becomes solvable.
///
/// Once the grid is solvable, many more slots may be wildcards than is actually necessary.
/// This class and the contained algorithm can remove unnecessary wildcards.
/// </summary>
internal static class WildcardSlotReducer
{
    public delegate void ReportMinimalCollapsibleWildcards(List<List<Slot>> minimalCollapsibleWildcards);
    public delegate void ReportCollapsible(bool isCollapsible);
    public delegate IEnumerable<CoroutineStatus> CreateCollapsibleCalculator(ReportCollapsible reportCollapsible);

    /// <summary>
    /// Start a coroutine that runs the algorithm removing the wildcards. It then reports minimal wildcard slots through <paramref name="reportMinimalWildcards"/>
    ///
    /// <paramref name="collapsibleCalculator"/> 
    /// </summary>
    public static IEnumerable<CoroutineStatus> StartRemovingWildcards(List<Slot> wildcardSlots, CreateCollapsibleCalculator collapsibleCalculator, ReportMinimalCollapsibleWildcards reportMinimalWildcards)
    {
        var minimalCollapsibleWildcards = new List<List<Slot>>();

        var collapseResultReceiver = new CollapseResultReceiver();

        foreach (var yieldResult in RemoveWildcardsRecursiveAndRecordMinimals(0, collapsibleCalculator, wildcardSlots, collapseResultReceiver, minimalCollapsibleWildcards))
        {
            yield return yieldResult;
        }

        reportMinimalWildcards(minimalCollapsibleWildcards);
    }

    private class CollapseResultReceiver
    {
        public bool isCollapsible = false;
    }

    private static IEnumerable<CoroutineStatus> RemoveWildcardsRecursiveAndRecordMinimals(int startIndex, CreateCollapsibleCalculator collapsibleCalculator, List<Slot> wildcardSlots, CollapseResultReceiver collapseResultReceiver, List<List<Slot>> outputMinimalWildcardSlots)
    {
        var anyRemovable = false;
        for (var wildcardSlotIndex = startIndex; wildcardSlotIndex < wildcardSlots.Count; ++wildcardSlotIndex)
        {
            var wildcardSlot = wildcardSlots[wildcardSlotIndex];

            // If there aren't any modules available for this cube config, the world will fail to collapse.
            // However, this invalidates some assumptions in the collapse algorithm and it would crash, so early out here.
            if (wildcardSlot.AvailableModules.Count > 0)
            {
                wildcardSlot.isWildcard = false;

                collapseResultReceiver.isCollapsible = false;
                foreach (var yieldResult in collapsibleCalculator(isCollapsible => collapseResultReceiver.isCollapsible = isCollapsible))
                {
                    yield return yieldResult;
                }

                if (collapseResultReceiver.isCollapsible)
                {
                    anyRemovable = true;

                    foreach (var yieldResult in RemoveWildcardsRecursiveAndRecordMinimals(wildcardSlotIndex + 1, collapsibleCalculator, wildcardSlots, collapseResultReceiver, outputMinimalWildcardSlots))
                    {
                        yield return yieldResult;
                    }
                }

                wildcardSlot.isWildcard = true;
            }
        }

        if (!anyRemovable)
        {
            AddIfNotSupersetOfExisting(outputMinimalWildcardSlots, wildcardSlots.Where(slot => slot.isWildcard));
        }
    }

    private static bool IsSupersetOfOtherWildcards(List<List<Slot>> minimalWildcards, IEnumerable<Slot> wildcards)
    {
        return minimalWildcards.Any(existingWildcardList => existingWildcardList.All(wildcardSlot => wildcards.Contains(wildcardSlot)));
    }

    private static void AddIfNotSupersetOfExisting(List<List<Slot>> minimalWildcards, IEnumerable<Slot> wildcards)
    {
        if (!IsSupersetOfOtherWildcards(minimalWildcards, wildcards))
        {
            minimalWildcards.Add(wildcards.ToList());
        }
    }
}

}

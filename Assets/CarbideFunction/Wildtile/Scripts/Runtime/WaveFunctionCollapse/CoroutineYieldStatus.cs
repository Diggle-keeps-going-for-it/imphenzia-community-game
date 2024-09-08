namespace CarbideFunction.Wildtile
{

/// <summary>
/// The process for building the map runs through distinct stages. This enum is used to indicate which stage the coroutine is in this process.
/// </summary>
public enum CoroutineStatus
{
    Propagating,
    CompletedCellCollapse,
    PerfectCollapseSucceeded,
    PerfectCollapseFailed,
    FoundCollapsableState,
    RemovedRedundantWildcards,
    Postprocessing,
}

}

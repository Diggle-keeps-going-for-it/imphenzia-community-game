
namespace CarbideFunction.Wildtile
{
    /// <summary>
    /// Delegates for reporting information from the propagation part of the wave function collapse algorithm.
    /// </summary>
    public static class PropagationReporting
    {
        /// <summary>
        /// Reports whether the propagation was successful or ran into a contradiction.
        /// </summary>
        public delegate void ReportPropagationResult(PropagationResult result);
        /// <summary>
        /// Whenever propagation removes the final available module from a slot, this delegate will be called with that slot.
        /// </summary>
        public delegate void ReportContradictionSlot(Slot firstContradictingSlot);
    }
}

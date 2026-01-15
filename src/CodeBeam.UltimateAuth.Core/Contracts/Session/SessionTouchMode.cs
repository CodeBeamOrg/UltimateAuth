namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public enum SessionTouchMode
    {
        /// <summary>
        /// Touch only if store policy allows (interval, throttling, etc.)
        /// </summary>
        IfNeeded,

        /// <summary>
        /// Always update session activity, ignoring store heuristics.
        /// </summary>
        Force
    }
}

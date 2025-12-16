namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record LoginContinuation
    {
        /// <summary>
        /// Gets the type of login continuation required.
        /// </summary>
        public LoginContinuationType Type { get; init; }

        /// <summary>
        /// Opaque continuation token used to resume the login flow.
        /// </summary>
        public string ContinuationToken { get; init; } = default!;

        /// <summary>
        /// Optional hint for UX (e.g. "Enter MFA code", "Verify device").
        /// </summary>
        public string? Hint { get; init; }
    }
}

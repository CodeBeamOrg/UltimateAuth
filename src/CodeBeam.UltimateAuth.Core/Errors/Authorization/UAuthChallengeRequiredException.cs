namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthChallengeRequiredException : UAuthException
    {
        public UAuthChallengeRequiredException(string? reason = null)
            : base(reason ?? "Additional authentication is required to perform this operation.")
        {
        }
    }
}

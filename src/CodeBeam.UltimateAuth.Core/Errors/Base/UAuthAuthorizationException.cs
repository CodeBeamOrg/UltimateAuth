namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthAuthorizationException : UAuthException
    {
        public UAuthAuthorizationException(string? reason = null)
            : base(reason ?? "The current principal is not authorized to perform this operation.")
        {
        }
    }
}

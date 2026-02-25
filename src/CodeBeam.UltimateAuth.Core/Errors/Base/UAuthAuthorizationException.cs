namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthAuthorizationException : UAuthRuntimeException
{
    public UAuthAuthorizationException(string? reason = null)
        : base(code: "forbidden", message: reason ?? "The current principal is not authorized to perform this operation.")
    {
    }
}

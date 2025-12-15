using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthSessionSecurityMismatchException : UAuthSessionException
{
    public long CurrentSecurityVersion { get; }

    public UAuthSessionSecurityMismatchException(
        AuthSessionId sessionId,
        long currentSecurityVersion)
        : base(
            sessionId,
            $"Session '{sessionId}' is invalid due to security version mismatch.")
    {
        CurrentSecurityVersion = currentSecurityVersion;
    }
}

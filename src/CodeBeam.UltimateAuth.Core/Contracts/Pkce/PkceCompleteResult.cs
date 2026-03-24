using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record PkceCompleteResult
{
    public bool Success { get; init; }

    public AuthFailureReason? FailureReason { get; init; }

    public LoginResult? LoginResult { get; init; }

    public bool InvalidPkce { get; init; }
}

using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record AuthValidationResult
{
    public required SessionState State { get; init; }
    public AuthStateSnapshot? Snapshot { get; init; }

    public bool IsValid => State == SessionState.Active;
}

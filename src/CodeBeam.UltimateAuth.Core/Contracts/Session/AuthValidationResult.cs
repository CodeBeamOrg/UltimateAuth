using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

// TODO: Validation protocol review after SemiHybrid/PureJwt: Re-evaluate exposure and necessity of
// ChainId, RootId, and BoundDeviceId in AuthValidationResult once all AuthModes have concrete validation semantics.
// Prefer the smallest common public contract and keep security lineage internal where possible.
public sealed record AuthValidationResult
{
    public required SessionState State { get; init; }
    public AuthStateSnapshot? Snapshot { get; init; }
    public SessionChainId? ChainId { get; init; }

    public SessionRootId? RootId { get; init; }

    public DeviceId? BoundDeviceId { get; init; }

    public bool IsValid => State == SessionState.Active;
}

using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class SessionChainDetail
{
    public SessionChainId ChainId { get; init; }

    public string? DeviceType { get; init; }
    public string? OperatingSystem { get; init; }
    public string? Platform { get; init; }
    public string? Browser { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastSeenAt { get; init; }

    public SessionChainState State { get; init; }

    public int RotationCount { get; init; }
    public int TouchCount { get; init; }

    public bool IsRevoked { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }

    public AuthSessionId? ActiveSessionId { get; init; }

    public IReadOnlyList<SessionInfo> Sessions { get; init; } = [];
}

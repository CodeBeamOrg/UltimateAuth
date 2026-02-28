using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionChainSummaryDto
{
    public required SessionChainId ChainId { get; init; }
    public string? DeviceName { get; init; }
    public string? DeviceType { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastSeenAt { get; init; }
    public int RotationCount { get; init; }
    public bool IsRevoked { get; init; }
    public AuthSessionId? ActiveSessionId { get; init; }
    public bool IsCurrentDevice { get; init; }
}

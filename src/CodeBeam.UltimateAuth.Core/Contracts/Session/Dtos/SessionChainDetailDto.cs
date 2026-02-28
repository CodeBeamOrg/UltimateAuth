using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionChainDetailDto(
    SessionChainId ChainId,
    string? DeviceName,
    string? DeviceType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSeenAt,
    int RotationCount,
    bool IsRevoked,
    AuthSessionId? ActiveSessionId,
    IReadOnlyList<SessionInfoDto> Sessions
    );

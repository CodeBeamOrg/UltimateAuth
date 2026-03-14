using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record SessionInfo(
    AuthSessionId SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsRevoked
    );

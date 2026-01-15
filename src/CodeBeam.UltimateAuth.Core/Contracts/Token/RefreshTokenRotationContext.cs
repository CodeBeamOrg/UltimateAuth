using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenRotationContext
{
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset Now { get; init; }
    public required DeviceContext Device { get; init; }
    public AuthSessionId? ExpectedSessionId { get; init; } // For Hybrid
}

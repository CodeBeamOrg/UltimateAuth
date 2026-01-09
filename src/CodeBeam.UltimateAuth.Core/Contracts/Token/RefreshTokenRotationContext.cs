using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenRotationContext<TUserId>
{
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset Now { get; init; }
    public DeviceInfo Device { get; init; } = DeviceInfo.Unknown;
    public AuthSessionId? ExpectedSessionId { get; init; } // For Hybrid
}

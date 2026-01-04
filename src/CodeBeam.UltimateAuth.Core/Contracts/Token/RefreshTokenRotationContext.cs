namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenRotationContext<TUserId>
{
    public string RefreshToken { get; init; } = default!;
    public DateTimeOffset Now { get; init; }
}

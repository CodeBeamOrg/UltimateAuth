using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshTokenRotationResult
{
    public bool IsSuccess { get; init; }
    public bool ReauthRequired { get; init; }
    public bool IsReuseDetected { get; init; } // internal use

    public AuthSessionId? SessionId { get; init; }
    public AccessToken? AccessToken { get; init; }
    public RefreshTokenInfo? RefreshToken { get; init; }

    private RefreshTokenRotationResult() { }

    public static RefreshTokenRotationResult Failed() => new() { IsSuccess = false, ReauthRequired = true };

    public static RefreshTokenRotationResult Success(
        AccessToken accessToken,
        RefreshTokenInfo refreshToken)
        => new()
        {
            IsSuccess = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
}

using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class RefreshFlowResult
{
    public bool Succeeded { get; init; }
    public RefreshOutcome Outcome { get; init; }

    public AuthSessionId? SessionId { get; init; }
    public AccessToken? AccessToken { get; init; }
    public RefreshTokenInfo? RefreshToken { get; init; }

    public static RefreshFlowResult ReauthRequired()
    {
        return new RefreshFlowResult
        {
            Succeeded = false,
            Outcome = RefreshOutcome.ReauthRequired
        };
    }

    public static RefreshFlowResult Success(
        RefreshOutcome outcome,
        AuthSessionId? sessionId = null,
        AccessToken? accessToken = null,
        RefreshTokenInfo? refreshToken = null)
    {
        return new RefreshFlowResult
        {
            Succeeded = true,
            Outcome = outcome,
            SessionId = sessionId,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

}

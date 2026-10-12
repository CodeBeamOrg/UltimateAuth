using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class RefreshFlowResult
{
    public bool IsSuccess =>
        Outcome is RefreshOutcome.Success
            or RefreshOutcome.NoOp
            or RefreshOutcome.Touched
            or RefreshOutcome.Rotated;

    public RefreshOutcome Outcome { get; init; }

    public AuthSessionId? SessionId { get; init; }
    public AccessToken? AccessToken { get; init; }
    public RefreshTokenInfo? RefreshToken { get; init; }

    public static RefreshFlowResult ReauthRequired()
    {
        return new RefreshFlowResult
        {
            Outcome = RefreshOutcome.ReauthRequired
        };
    }

    public static RefreshFlowResult Success(
        RefreshOutcome outcome,
        AuthSessionId? sessionId = null,
        AccessToken? accessToken = null,
        RefreshTokenInfo? refreshToken = null)
    {

        if (outcome is not (RefreshOutcome.Success or RefreshOutcome.NoOp or RefreshOutcome.Touched or RefreshOutcome.Rotated))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        return new RefreshFlowResult
        {
            Outcome = outcome,
            SessionId = sessionId,
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }

}

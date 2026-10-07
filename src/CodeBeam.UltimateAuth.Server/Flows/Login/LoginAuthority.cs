using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Default implementation of the login authority.
/// Applies basic security checks for login attempts.
/// </summary>
public sealed class LoginAuthority : ILoginAuthority
{
    private readonly IClock _clock;

    public LoginAuthority(IClock clock)
    {
        _clock = clock;
    }

    public LoginDecision Decide(LoginDecisionContext context)
    {
        if (!context.UserExists || context.UserKey is null)
        {
            return LoginDecision.Deny(AuthFailureReason.InvalidCredentials);
        }

        var state = context.SecurityState;
        if (state is not null)
        {
            if (state.IsLocked(_clock.UtcNow))
                return LoginDecision.Deny(AuthFailureReason.LockedOut);

            if (state.RequiresReauthentication)
                return LoginDecision.Challenge(AuthFailureReason.ReauthenticationRequired);
        }

        if (!context.CredentialsValid)
        {
            return LoginDecision.Deny(AuthFailureReason.InvalidCredentials);
        }

        return LoginDecision.Allow();
    }
}

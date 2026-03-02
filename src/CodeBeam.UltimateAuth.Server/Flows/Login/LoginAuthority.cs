using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Default implementation of the login authority.
/// Applies basic security checks for login attempts.
/// </summary>
public sealed class LoginAuthority : ILoginAuthority
{
    public LoginDecision Decide(LoginDecisionContext context)
    {
        if (!context.UserExists || context.UserKey is null)
        {
            return LoginDecision.Deny(AuthFailureReason.InvalidCredentials);
        }

        var state = context.SecurityState;
        if (state is not null)
        {
            if (state.IsLocked(DateTimeOffset.UtcNow))
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

using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Default implementation of the login authority.
/// Applies basic security checks for login attempts.
/// </summary>
public sealed class LoginAuthority : ILoginAuthority
{
    private readonly UAuthLoginOptions _options;

    public LoginAuthority(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.Login;
    }

    public LoginDecision Decide(LoginDecisionContext context)
    {
        if (!context.UserExists || context.UserKey is null)
        {
            return LoginDecision.Deny("Invalid credentials.");
        }

        var state = context.SecurityState;
        if (state is not null)
        {
            if (state.IsLocked)
                return LoginDecision.Deny("user_is_locked");

            if (state.RequiresReauthentication)
                return LoginDecision.Challenge("reauth_required");
        }

        if (!context.CredentialsValid)
        {
            return LoginDecision.Deny("Invalid credentials.");
        }

        return LoginDecision.Allow();
    }
}

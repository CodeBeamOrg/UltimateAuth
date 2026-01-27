namespace CodeBeam.UltimateAuth.Server.Login
{
    /// <summary>
    /// Default implementation of the login authority.
    /// Applies basic security checks for login attempts.
    /// </summary>
    public sealed class DefaultLoginAuthority : ILoginAuthority
    {
        public LoginDecision Decide(LoginDecisionContext context)
        {
            if (!context.CredentialsValid)
            {
                return LoginDecision.Deny("Invalid credentials.");
            }

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

            return LoginDecision.Allow();
        }
    }
}

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
            // 1. Credentials must be valid
            if (!context.CredentialsValid)
            {
                return LoginDecision.Deny("Invalid credentials.");
            }

            // 2. User must exist
            if (!context.UserExists || context.UserKey is null)
            {
                // Deliberately vague to prevent user enumeration
                return LoginDecision.Deny("Invalid credentials.");
            }

            // 3. User must be active and not locked
            if (context.SecurityState is not null)
            {
                if (context.SecurityState.IsLocked)
                {
                    return LoginDecision.Deny("User account is locked.");
                }

                if (context.SecurityState.RequiresReauthentication)
                {
                    return LoginDecision.Challenge("Reauthentication required.");
                }
            }

            // 4. All checks passed
            return LoginDecision.Allow();
        }
    }
}

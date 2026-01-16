namespace CodeBeam.UltimateAuth.Server.Login
{
    /// <summary>
    /// Represents the authority responsible for making login decisions.
    /// This authority determines whether a login attempt is allowed,
    /// denied, or requires additional verification (e.g. MFA).
    /// </summary>
    public interface ILoginAuthority
    {
        /// <summary>
        /// Evaluates a login attempt based on the provided decision context.
        /// </summary>
        /// <param name="context">The login decision context.</param>
        /// <returns>The login decision.</returns>
        LoginDecision Decide(LoginDecisionContext context);
    }
}

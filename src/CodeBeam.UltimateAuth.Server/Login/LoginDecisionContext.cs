using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Server.Login
{
    /// <summary>
    /// Represents all information required by the login authority
    /// to make a login decision.
    /// </summary>
    public sealed class LoginDecisionContext
    {
        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public TenantKey Tenant { get; init; }

        /// <summary>
        /// Gets the login identifier (e.g. username or email).
        /// </summary>
        public required string Identifier { get; init; }

        /// <summary>
        /// Indicates whether the provided credentials were successfully validated.
        /// </summary>
        public bool CredentialsValid { get; init; }

        /// <summary>
        /// Gets the resolved user identifier if available.
        /// </summary>
        public UserKey? UserKey { get; init; }

        /// <summary>
        /// Gets the user security state if the user could be resolved.
        /// </summary>
        public IUserSecurityState? SecurityState { get; init; }

        /// <summary>
        /// Indicates whether the user exists.
        /// This allows the authority to distinguish between
        /// invalid credentials and non-existent users.
        /// </summary>
        public bool UserExists { get; init; }

        /// <summary>
        /// Indicates whether this login attempt is part of a chained flow
        /// (e.g. reauthentication, MFA completion).
        /// </summary>
        public bool IsChained { get; init; }
    }
}

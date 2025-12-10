namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides minimal user lookup and security metadata required for authentication.
    /// This store does not manage user creation, claims, or profile data — these belong
    /// to higher-level application services outside UltimateAuth.
    /// </summary>
    public interface IUserStore<TUserId>
    {
        /// <summary>
        /// Retrieves a user by identifier. Returns <c>null</c> if no such user exists.
        /// </summary>
        /// <param name="userId">The identifier of the user.</param>
        /// <returns>The user instance or <c>null</c> if not found.</returns>
        Task<IUser<TUserId>?> FindByIdAsync(TUserId userId);

        /// <summary>
        /// Retrieves a user by a login credential such as username or email.
        /// Returns <c>null</c> if no matching user exists.
        /// </summary>
        /// <param name="login">The login value used to locate the user.</param>
        /// <returns>The user instance or <c>null</c> if not found.</returns>
        Task<IUser<TUserId>?> FindByLoginAsync(string login);

        /// <summary>
        /// Returns the password hash for the specified user, if the user participates
        /// in password-based authentication. Returns <c>null</c> for passwordless users
        /// (e.g., external login or passkey-only accounts).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The password hash or <c>null</c>.</returns>
        Task<string?> GetPasswordHashAsync(TUserId userId);

        /// <summary>
        /// Updates the password hash for the specified user. This method is invoked by
        /// password management services and not by <see cref="ISessionService{TUserId}"/>.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="passwordHash">The new password hash value.</param>
        Task SetPasswordHashAsync(TUserId userId, string passwordHash);

        /// <summary>
        /// Retrieves the security version associated with the user.
        /// This value increments whenever critical security actions occur, such as:
        /// password reset, MFA reset, external login removal, or account recovery.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The current security version.</returns>
        Task<long> GetSecurityVersionAsync(TUserId userId);

        /// <summary>
        /// Increments the user's security version, invalidating all existing sessions.
        /// This is typically called after sensitive security events occur.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task IncrementSecurityVersionAsync(TUserId userId);
    }
}

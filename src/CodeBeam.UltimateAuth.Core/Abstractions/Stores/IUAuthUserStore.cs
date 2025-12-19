using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides minimal user lookup and security metadata required for authentication.
    /// This store does not manage user creation, claims, or profile data — these belong
    /// to higher-level application services outside UltimateAuth.
    /// </summary>
    public interface IUAuthUserStore<TUserId>
    {
        Task<IUser<TUserId>?> FindByIdAsync(string? tenantId, TUserId userId, CancellationToken token = default);

        Task<UserRecord<TUserId>?> FindByUsernameAsync(string? tenantId, string username, CancellationToken ct = default);

        /// <summary>
        /// Retrieves a user by a login credential such as username or email.
        /// Returns <c>null</c> if no matching user exists.
        /// </summary>
        /// <param name="login">The login value used to locate the user.</param>
        /// <returns>The user instance or <c>null</c> if not found.</returns>
        Task<IUser<TUserId>?> FindByLoginAsync(string? tenantId, string login, CancellationToken token = default);

        /// <summary>
        /// Returns the password hash for the specified user, if the user participates
        /// in password-based authentication. Returns <c>null</c> for passwordless users
        /// (e.g., external login or passkey-only accounts).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The password hash or <c>null</c>.</returns>
        Task<string?> GetPasswordHashAsync(string? tenantId, TUserId userId, CancellationToken token = default);

        /// <summary>
        /// Updates the password hash for the specified user. This method is invoked by
        /// password management services and not by <see cref="IUAuthSessionService{TUserId}"/>.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="passwordHash">The new password hash value.</param>
        Task SetPasswordHashAsync(string? tenantId, TUserId userId, string passwordHash, CancellationToken token = default);

        /// <summary>
        /// Retrieves the security version associated with the user.
        /// This value increments whenever critical security actions occur, such as:
        /// password reset, MFA reset, external login removal, or account recovery.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The current security version.</returns>
        Task<long> GetSecurityVersionAsync(string? tenantId, TUserId userId, CancellationToken token = default);

        /// <summary>
        /// Increments the user's security version, invalidating all existing sessions.
        /// This is typically called after sensitive security events occur.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        Task IncrementSecurityVersionAsync(string? tenantId, TUserId userId, CancellationToken token = default);
    }
}

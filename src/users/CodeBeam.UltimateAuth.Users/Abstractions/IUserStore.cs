using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Users
{
    public interface IUserStore<TUserId>
    {
        /// <summary>
        /// Finds a user by its application-level user id.
        /// Returns null if the user does not exist or is deleted.
        /// </summary>
        Task<UserRecord<TUserId>?> FindByIdAsync(string? tenantId, TUserId userId, CancellationToken ct = default);

        /// <summary>
        /// Finds a user by a login identifier (username, email, etc).
        /// Used during login discovery phase.
        /// </summary>
        Task<UserRecord<TUserId>?> FindByLoginAsync(string? tenantId, string login, CancellationToken ct = default);

        /// <summary>
        /// Checks whether a user exists.
        /// Fast-path helper for authorities.
        /// </summary>
        Task<bool> ExistsAsync(string? tenantId, TUserId userId, CancellationToken ct = default);
    }
}

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides basic user lookup and security metadata required for authentication. 
    /// 
    /// This store does NOT manage passwords, claims, or user creation — those belong to higher-level application services.
    /// </summary>
    public interface IUserStore<TUserId>
    {
        /// <summary>
        /// Retrieve user information by id.
        /// Return null if user does not exist.
        /// </summary>
        Task<IUser<TUserId>?> FindByIdAsync(TUserId userId);

        /// <summary>
        /// Retrieve user by username, email or any identifier used for login.
        /// Return null if not found.
        /// </summary>
        Task<IUser<TUserId>?> FindByLoginAsync(string login);

        /// <summary>
        /// Returns the password hash for the user, if applicable.
        /// If the user does not use password-based authentication, return null.
        /// </summary>
        Task<string?> GetPasswordHashAsync(TUserId userId);

        /// <summary>
        /// Updates the password hash value.
        /// This is called by PasswordService (not by SessionService).
        /// </summary>
        Task SetPasswordHashAsync(TUserId userId, string passwordHash);

        /// <summary>
        /// Returns the security version of the user.
        /// This value increments when critical security changes occur:
        /// - password reset
        /// - MFA reset
        /// - external login removed
        /// - account recovery
        /// </summary>
        Task<long> GetSecurityVersionAsync(TUserId userId);

        /// <summary>
        /// Updates the security version.
        /// This forces all existing sessions to become invalid.
        /// </summary>
        Task IncrementSecurityVersionAsync(TUserId userId);
    }
}

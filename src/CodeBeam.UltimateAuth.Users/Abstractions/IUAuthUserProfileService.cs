namespace CodeBeam.UltimateAuth.Server.Users
{
    /// <summary>
    /// User self-service operations (profile, password, MFA).
    /// </summary>
    public interface IUAuthUserProfileService<TUserId>
    {
        Task<UserProfileDto<TUserId>> GetCurrentAsync(
            CancellationToken ct = default);

        Task UpdateProfileAsync(
            UpdateProfileRequest request,
            CancellationToken ct = default);

        Task ChangePasswordAsync(
            ChangePasswordRequest request,
            CancellationToken ct = default);

        Task ConfigureMfaAsync(
            ConfigureMfaRequest request,
            CancellationToken ct = default);
    }
}

namespace CodeBeam.UltimateAuth.Server.Users
{
    /// <summary>
    /// Administrative user management operations.
    /// </summary>
    public interface IUAuthUserManagementService<TUserId>
    {
        Task<UserDto<TUserId>> GetByIdAsync(
            TUserId userId,
            CancellationToken ct = default);

        Task<IReadOnlyList<UserDto<TUserId>>> GetAllAsync(
            CancellationToken ct = default);

        Task DisableAsync(
            TUserId userId,
            CancellationToken ct = default);

        Task EnableAsync(
            TUserId userId,
            CancellationToken ct = default);

        Task ResetPasswordAsync(
            TUserId userId,
            ResetPasswordRequest request,
            CancellationToken ct = default);
    }
}

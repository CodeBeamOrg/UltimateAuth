using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Minimal user operations required for authentication.
    /// Does NOT include role or permission management.
    /// For user management, CodeBeam.UltimateAuth.Users package is recommended.
    /// </summary>
    public interface IUAuthUserService<TUserId>
    {
        Task<TUserId> RegisterAsync(RegisterUserRequest request, CancellationToken cancellationToken = default);

        Task DeleteAsync(TUserId userId, CancellationToken cancellationToken = default);

        Task<bool> ValidateCredentialsAsync(ValidateCredentialsRequest request, CancellationToken cancellationToken = default);

        Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(string? tenantId, string identifier, string secret, CancellationToken cancellationToken = default);
    }
}

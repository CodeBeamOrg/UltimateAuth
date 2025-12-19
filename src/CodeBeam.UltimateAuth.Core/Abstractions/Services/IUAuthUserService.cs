using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Defines the minimal user authentication contract expected by UltimateAuth.
    /// This service does not manage sessions, tokens, or transport concerns.
    /// For user management, CodeBeam.UltimateAuth.Users package is recommended.
    /// </summary>
    public interface IUAuthUserService<TUserId>
    {
        Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(string? tenantId, string identifier, string secret, CancellationToken cancellationToken = default);
        Task<bool> ValidateCredentialsAsync(ValidateCredentialsRequest request, CancellationToken cancellationToken = default);
    }
}

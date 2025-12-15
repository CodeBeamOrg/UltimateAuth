using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUserAuthenticator<TUserId>
    {
        Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(
            string? tenantId,
            string identifier,
            string secret,
            CancellationToken cancellationToken = default);
    }
}

using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IUserAuthenticator<TUserId>
    {
        Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(string? tenantId, AuthenticationContext context, CancellationToken ct = default);
    }
}

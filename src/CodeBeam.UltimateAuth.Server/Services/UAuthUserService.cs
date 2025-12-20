using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Users
{
    internal sealed class UAuthUserService<TUserId> : IUAuthUserService<TUserId>
    {
        private readonly IUserAuthenticator<TUserId> _authenticator;

        public UAuthUserService(IUserAuthenticator<TUserId> authenticator)
        {
            _authenticator = authenticator;
        }

        public async Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(string? tenantId, string identifier, string secret, CancellationToken ct = default)
        {
            var context = new AuthenticationContext
            {
                Identifier = identifier,
                Secret = secret,
                CredentialType = "password"
            };

            return await _authenticator.AuthenticateAsync(tenantId, context, ct);
        }

        // This method must not issue sessions or tokens
        public async Task<bool> ValidateCredentialsAsync(ValidateCredentialsRequest request, CancellationToken ct = default)
        {
            var context = new AuthenticationContext
            {
                Identifier = request.Identifier,
                Secret = request.Password,
                CredentialType = "password"
            };

            var result = await _authenticator.AuthenticateAsync(request.TenantId,context, ct);
            return result.Succeeded;
        }
    }
}

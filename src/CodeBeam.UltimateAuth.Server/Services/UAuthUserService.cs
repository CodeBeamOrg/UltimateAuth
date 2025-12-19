using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Users;

internal sealed class UAuthUserService<TUserId> : IUAuthUserService<TUserId>
{
    private readonly IUserAuthenticator<TUserId> _authenticator;

    public UAuthUserService(IUserAuthenticator<TUserId> authenticator)
    {
        _authenticator = authenticator;
    }

    public async Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(string? tenantId, string identifier, string secret, CancellationToken ct = default)
    {
        return await _authenticator.AuthenticateAsync(tenantId, identifier, secret, ct);
    }

    // This method must not issue sessions or tokens
    public async Task<bool> ValidateCredentialsAsync(ValidateCredentialsRequest request, CancellationToken ct = default)
    {
        
        var result = await _authenticator.AuthenticateAsync(request.TenantId, request.Identifier, request.Password, ct);
        return result.Succeeded;
    }

}


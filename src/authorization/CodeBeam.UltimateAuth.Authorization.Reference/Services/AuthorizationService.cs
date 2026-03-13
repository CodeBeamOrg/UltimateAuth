using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Authorization.Reference;

internal sealed class AuthorizationService : IAuthorizationService
{
    private readonly IAccessOrchestrator _accessOrchestrator;

    public AuthorizationService(IAccessOrchestrator accessOrchestrator)
    {
        _accessOrchestrator = accessOrchestrator;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<AuthorizationResult>(innerCt =>
        {
            return Task.FromResult(AuthorizationResult.Allow());
        });

        try
        {
            await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
            return AuthorizationResult.Allow();
        }
        catch (UAuthAuthorizationException ex)
        {
            return AuthorizationResult.Deny(ex.Message);
        }
    }
}

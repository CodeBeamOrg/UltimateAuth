using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Authorization.Reference
{
    public interface IAuthorizationService
    {
        Task<AuthorizationResult> AuthorizeAsync(string? tenantId, AccessContext context, CancellationToken ct = default);
    }

}

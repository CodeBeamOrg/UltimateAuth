using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserIdentifierService
    {
        Task<GetUserIdentifiersResult> GetAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
        Task<IdentifierChangeResult> ChangeAsync(string? tenantId, UserKey userKey, ChangeUserIdentifierRequest request, CancellationToken ct = default);
        Task<IdentifierVerificationResult> VerifyAsync(string? tenantId, UserKey userKey, VerifyUserIdentifierRequest request, CancellationToken ct = default);
        Task<IdentifierDeleteResult> DeleteAsync(string? tenantId, UserKey userKey, DeleteUserIdentifierRequest request, CancellationToken ct = default);
    }
}

using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference
{
    public interface IUserIdentifierService
    {
        Task<GetUserIdentifiersResult> GetAsync(AccessContext context, UserKey targetUserKey, CancellationToken ct = default);
        Task<IdentifierChangeResult> ChangeAsync(AccessContext context, UserKey targetUserKey, ChangeUserIdentifierRequest request, CancellationToken ct = default);
        Task<IdentifierVerificationResult> VerifyAsync(AccessContext context, UserKey targetUserKey, VerifyUserIdentifierRequest request, CancellationToken ct = default);
        Task<IdentifierDeleteResult> DeleteAsync(AccessContext context, UserKey targetUserKey, DeleteUserIdentifierRequest request, CancellationToken ct = default);
    }
}

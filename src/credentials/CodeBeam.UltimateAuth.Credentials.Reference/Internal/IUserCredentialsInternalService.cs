using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference.Internal
{
    internal interface IUserCredentialsInternalService
    {
        Task<CredentialActionResult> DeleteInternalAsync(string? tenantId, UserKey userKey, CancellationToken ct = default);
    }
}

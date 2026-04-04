using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference.Internal;

internal interface IUserCredentialsInternalService
{
    Task<CredentialActionResult> DeleteInternalAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default);
}

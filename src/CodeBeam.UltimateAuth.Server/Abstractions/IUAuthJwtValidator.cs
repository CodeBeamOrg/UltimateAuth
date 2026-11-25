using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Contracts;

namespace CodeBeam.UltimateAuth.Server.Abstractions;

public interface IUAuthJwtValidator
{
    Task<ValidatedJwtIdentity?> ValidateAsync(string token, TenantKey expectedTenant, CancellationToken ct = default);
}

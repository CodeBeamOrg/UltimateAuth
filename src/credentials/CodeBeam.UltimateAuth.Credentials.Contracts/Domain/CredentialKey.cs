using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public readonly record struct CredentialKey(
    TenantKey Tenant,
    Guid Id);

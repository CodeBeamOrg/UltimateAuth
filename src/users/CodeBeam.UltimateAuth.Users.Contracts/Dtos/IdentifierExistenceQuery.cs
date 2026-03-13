using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierExistenceQuery(
    TenantKey Tenant,
    UserIdentifierType Type,
    string NormalizedValue,
    IdentifierExistenceScope Scope,
    UserKey? UserKey = null,
    Guid? ExcludeIdentifierId = null
    );

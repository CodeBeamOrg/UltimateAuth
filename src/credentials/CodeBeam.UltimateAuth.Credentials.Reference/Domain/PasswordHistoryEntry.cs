using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

public sealed record PasswordHistoryEntry
{
    public required Guid Id { get; init; }

    public required TenantKey Tenant { get; init; }
    public required UserKey UserKey { get; init; }

    public required CredentialKey CredentialKey { get; init; }

    public required PasswordHash SecretHash { get; init; }

    public required DateTimeOffset ReplacedAt { get; init; }
}

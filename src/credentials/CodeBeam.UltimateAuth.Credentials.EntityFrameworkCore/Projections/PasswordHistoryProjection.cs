using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public sealed class PasswordHistoryProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }
    public UserKey UserKey { get; set; }

    public Guid CredentialId { get; set; }

    public PasswordHash SecretHash { get; set; }

    public DateTimeOffset ReplacedAt { get; set; }
}

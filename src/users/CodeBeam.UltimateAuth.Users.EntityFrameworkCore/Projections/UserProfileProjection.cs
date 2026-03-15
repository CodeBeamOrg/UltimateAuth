using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal sealed class UserProfileProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; set; } = default!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? DisplayName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string? Gender { get; set; }

    public string? Bio { get; set; }

    public string? Language { get; set; }

    public string? TimeZone { get; set; }

    public string? Culture { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}

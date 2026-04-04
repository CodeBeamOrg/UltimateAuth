using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Authorization;

public sealed class Role : ITenantEntity, IVersionedEntity, IEntitySnapshot<Role>, ISoftDeletable<Role>
{
    private readonly HashSet<Permission> _permissions = new();

    public RoleId Id { get; private set; }
    public TenantKey Tenant { get; private set; }

    public string Name { get; private set; } = default!;
    public string NormalizedName { get; private set; } = default!;

    public IReadOnlyCollection<Permission> Permissions => _permissions;

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public long Version { get; set; }

    public bool IsDeleted => DeletedAt != null;

    private Role() { }

    public static Role Create(
        RoleId? id,
        TenantKey tenant,
        string name,
        IEnumerable<Permission>? permissions,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new UAuthValidationException("role_name_required");

        var normalized = Normalize(name);

        var role = new Role
        {
            Id = id ?? RoleId.New(),
            Tenant = tenant,
            Name = name.Trim(),
            NormalizedName = normalized,
            CreatedAt = now,
            Version = 0
        };

        if (permissions is not null)
        {
            foreach (var p in permissions)
                role._permissions.Add(p);
        }

        return role;
    }

    public Role Rename(string newName, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new UAuthValidationException("role_name_required");

        if (NormalizedName == Normalize(newName))
            return this;

        Name = newName.Trim();
        NormalizedName = Normalize(newName);
        UpdatedAt = now;

        return this;
    }

    public Role AddPermission(Permission permission, DateTimeOffset now)
    {
        _permissions.Add(permission);
        UpdatedAt = now;
        return this;
    }

    public Role RemovePermission(Permission permission, DateTimeOffset now)
    {
        _permissions.Remove(permission);
        UpdatedAt = now;
        return this;
    }

    public Role SetPermissions(IEnumerable<Permission> permissions, DateTimeOffset now)
    {
        _permissions.Clear();
        var normalized = PermissionNormalizer.Normalize(permissions, UAuthPermissionCatalog.GetAdminPermissions());

        foreach (var p in normalized)
            _permissions.Add(p);

        UpdatedAt = now;
        return this;
    }

    public Role MarkDeleted(DateTimeOffset now)
    {
        if (IsDeleted)
            return this;

        DeletedAt = now;
        UpdatedAt = now;

        return this;
    }

    public Role Snapshot()
    {
        var copy = new Role
        {
            Id = Id,
            Tenant = Tenant,
            Name = Name,
            NormalizedName = NormalizedName,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DeletedAt = DeletedAt,
            Version = Version
        };

        foreach (var p in _permissions)
            copy._permissions.Add(p);

        return copy;
    }

    public static Role FromProjection(
        RoleId id,
        TenantKey tenant,
        string name,
        IEnumerable<Permission> permissions,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt,
        DateTimeOffset? deletedAt,
        long version)
    {
        var role = new Role
        {
            Id = id,
            Tenant = tenant,
            Name = name,
            NormalizedName = Normalize(name),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            Version = version
        };

        foreach (var p in permissions)
            role._permissions.Add(p);

        return role;
    }

    private static string Normalize(string name)
        => name.Trim().ToUpperInvariant();
}
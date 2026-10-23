using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.IdentityLab.Seed;

public sealed class LabAdminSeedContributor(
    IUserLifecycleStoreFactory lifecycleFactory,
    IUserProfileStoreFactory profileFactory,
    IUserIdentifierStoreFactory identifierFactory,
    IPasswordCredentialStoreFactory credentialFactory,
    IRoleStoreFactory roleStoreFactory,
    IUserRoleStoreFactory userRoleStoreFactory,
    IIdentifierNormalizer identifierNormalizer,
    IUAuthPasswordHasher passwordHasher,
    IClock clock,
    IOptions<LabAdminOptions> options) : ISeedContributor
{
    private static readonly Guid PasswordCredentialId = Guid.Parse("df5d4ed3-6f80-4a85-9bb0-53130e76d2a1");

    public int Order => 0;

    public async Task SeedAsync(TenantKey tenant, CancellationToken ct = default)
    {
        var settings = options.Value;

        if (string.IsNullOrWhiteSpace(settings.Username) ||
            string.IsNullOrWhiteSpace(settings.Email) ||
            string.IsNullOrWhiteSpace(settings.Password))
        {
            throw new InvalidOperationException("LabAdmin username, email and password must be configured.");
        }

        var userKey = LabAdminIdentity.UserKey;

        await EnsureUserAsync(tenant, userKey, settings, ct);
        await EnsureCredentialAsync(tenant, userKey, settings.Password, ct);
        await EnsureSystemRolesAsync(tenant, ct);
        await EnsureAdminRoleAssignmentAsync(tenant, userKey, ct);
    }

    private async Task EnsureUserAsync(
        TenantKey tenant,
        UserKey userKey,
        LabAdminOptions settings,
        CancellationToken ct)
    {
        var now = clock.UtcNow;

        var lifecycleStore = lifecycleFactory.Create(tenant);
        var profileStore = profileFactory.Create(tenant);
        var identifierStore = identifierFactory.Create(tenant);

        var lifecycleKey = new UserLifecycleKey(tenant, userKey);

        if (!await lifecycleStore.ExistsAsync(lifecycleKey, ct))
        {
            await lifecycleStore.AddAsync(
                UserLifecycle.Create(tenant, userKey, now),
                ct);
        }

        var profileKey = new UserProfileKey(
            tenant, userKey, ProfileKey.Default);

        if (!await profileStore.ExistsAsync(profileKey, ct))
        {
            await profileStore.AddAsync(
                UserProfile.Create(
                    Guid.NewGuid(),
                    tenant,
                    userKey,
                    ProfileKey.Default,
                    now,
                    firstName: "Lab",
                    lastName: "Administrator",
                    displayName: settings.DisplayName),
                ct);
        }

        await EnsureIdentifierAsync(
            identifierStore,
            tenant,
            userKey,
            UserIdentifierType.Username,
            settings.Username,
            now,
            ct);

        await EnsureIdentifierAsync(
            identifierStore,
            tenant,
            userKey,
            UserIdentifierType.Email,
            settings.Email,
            now,
            ct);
    }

    private async Task EnsureIdentifierAsync(
        IUserIdentifierStore store,
        TenantKey tenant,
        UserKey userKey,
        UserIdentifierType type,
        string value,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var normalized = identifierNormalizer
            .Normalize(type, value).Normalized;

        var existing = await store.GetAsync(
            type, normalized, ct);

        if (existing is not null)
        {
            if (existing.UserKey != userKey)
                throw new InvalidOperationException(
                    $"LabAdmin {type} is already assigned to another user.");

            return;
        }

        await store.AddAsync(
            UserIdentifier.Create(
                Guid.NewGuid(),
                tenant,
                userKey,
                type,
                value,
                normalized,
                now,
                true,
                now),
            ct);
    }

    private async Task EnsureCredentialAsync(
        TenantKey tenant,
        UserKey userKey,
        string password,
        CancellationToken ct)
    {
        var store = credentialFactory.Create(tenant);

        var existing = await store.GetByUserAsync(userKey, ct);

        // Never overwrite an existing administrator password.
        if (existing.Any())
            return;

        await store.AddAsync(
            PasswordCredential.Create(
                PasswordCredentialId,
                tenant,
                userKey,
                passwordHasher.Hash(password),
                CredentialSecurityState.Active(),
                new CredentialMetadata(),
                clock.UtcNow),
            ct);
    }

    // TODO(Part 3 - Security):
    // Owner wildcard permission is temporary and intended only
    // for local Identity Lab happy-path testing.
    //
    // Replace wildcard access with resource-scoped authorization:
    // - Validate sandbox membership and ownership.
    // - Restrict user operations to the current sandbox.
    // - Enforce sandbox scope at the UltimateAuth endpoint/request level.
    // - Prevent cross-sandbox access through direct API calls.
    // - Replace wildcard with explicit Owner permissions.
    //
    // SECURITY: Do not deploy publicly while this wildcard is active.
    private async Task EnsureSystemRolesAsync(TenantKey tenant, CancellationToken ct)
    {
        var roleStore = roleStoreFactory.Create(tenant);

        foreach (var roleName in LabRoles.SystemRoles)
        {
            var existing = await roleStore.GetByNameAsync(roleName.ToUpperInvariant(), ct);

            if (existing is not null)
                continue;

            var permissions = roleName is LabRoles.Admin or LabRoles.Owner
                ? new HashSet<Permission> { Permission.Wildcard }
                : new HashSet<Permission>();

            var role = Role.Create(
                RoleId.From(Guid.NewGuid()),
                tenant,
                roleName,
                permissions,
                clock.UtcNow);

            await roleStore.AddAsync(role, ct);
        }
    }

    private async Task EnsureAdminRoleAssignmentAsync(TenantKey tenant, UserKey userKey, CancellationToken ct)
    {
        var roleStore = roleStoreFactory.Create(tenant);
        var userRoleStore = userRoleStoreFactory.Create(tenant);

        var role = await roleStore.GetByNameAsync(LabRoles.Admin.ToUpperInvariant(), ct);

        if (role is null)
            throw new InvalidOperationException("LabAdmin role was not initialized.");

        var assignments = await userRoleStore.GetAssignmentsAsync(userKey, ct);

        if (assignments.Any(x => x.RoleId == role.Id))
            return;

        await userRoleStore.AssignAsync(userKey, role.Id, clock.UtcNow, ct);
    }
}

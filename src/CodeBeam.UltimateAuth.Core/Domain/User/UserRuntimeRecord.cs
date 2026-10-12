namespace CodeBeam.UltimateAuth.Core.Domain;

// TODO (User Management Expansion):
// UltimateAuth must not require host applications to inherit from framework
// user types or implement interfaces on their existing user entities.
//
// Host applications should integrate their user models through dedicated
// adapter/provider interfaces registered via dependency injection.
//
// Future User Management Expansion work should define and document these
// integration contracts, including user resolution, runtime state mapping,
// lifecycle integration, and claims provisioning.
//
// Keep host user entities independent from UltimateAuth contracts.
// Do not introduce a mandatory IUser or IAuthSubject dependency.

/// <summary>
/// Represents the minimal user state required by UltimateAuth at runtime.
/// This is a framework-owned snapshot, not a host application user entity.
/// </summary>
public sealed record UserRuntimeRecord
{
    public UserKey UserKey { get; init; }
    public bool IsActive { get; init; }
    public bool CanAuthenticate { get; init; }
    public bool IsDeleted { get; init; }
    public bool Exists { get; init; }
}

using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record IdentifierExistenceResult(
    bool Exists,
    UserKey? OwnerUserKey = null,
    Guid? OwnerIdentifierId = null,
    bool OwnerIsPrimary = false
    );

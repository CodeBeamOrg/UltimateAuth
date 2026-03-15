using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal static class UserIdentifierMapper
{
    public static UserIdentifier ToDomain(this UserIdentifierProjection p)
    {
        return UserIdentifier.FromProjection(
            p.Id,
            p.Tenant,
            p.UserKey,
            p.Type,
            p.Value,
            p.NormalizedValue,
            p.IsPrimary,
            p.CreatedAt,
            p.VerifiedAt,
            p.UpdatedAt,
            p.DeletedAt,
            p.Version);
    }

    public static UserIdentifierProjection ToProjection(this UserIdentifier d)
    {
        return new UserIdentifierProjection
        {
            Id = d.Id,
            Tenant = d.Tenant,
            UserKey = d.UserKey,
            Type = d.Type,
            Value = d.Value,
            NormalizedValue = d.NormalizedValue,
            IsPrimary = d.IsPrimary,
            CreatedAt = d.CreatedAt,
            VerifiedAt = d.VerifiedAt,
            UpdatedAt = d.UpdatedAt,
            DeletedAt = d.DeletedAt,
            Version = d.Version
        };
    }
}

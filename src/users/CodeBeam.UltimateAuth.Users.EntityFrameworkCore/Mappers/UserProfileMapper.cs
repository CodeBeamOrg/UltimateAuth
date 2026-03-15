using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

internal static class UserProfileMapper
{
    public static UserProfile ToDomain(this UserProfileProjection p)
    {
        return UserProfile.FromProjection(
            p.Id,
            p.Tenant,
            p.UserKey,
            p.FirstName,
            p.LastName,
            p.DisplayName,
            p.BirthDate,
            p.Gender,
            p.Bio,
            p.Language,
            p.TimeZone,
            p.Culture,
            p.Metadata,
            p.CreatedAt,
            p.UpdatedAt,
            p.DeletedAt,
            p.Version);
    }

    public static UserProfileProjection ToProjection(this UserProfile d)
    {
        return new UserProfileProjection
        {
            Id = d.Id,
            Tenant = d.Tenant,
            UserKey = d.UserKey,
            FirstName = d.FirstName,
            LastName = d.LastName,
            DisplayName = d.DisplayName,
            BirthDate = d.BirthDate,
            Gender = d.Gender,
            Bio = d.Bio,
            Language = d.Language,
            TimeZone = d.TimeZone,
            Culture = d.Culture,
            Metadata = d.Metadata?.ToDictionary(x => x.Key, x => x.Value),
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            DeletedAt = d.DeletedAt,
            Version = d.Version
        };
    }
}

using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference.Domain;

namespace CodeBeam.UltimateAuth.Users.Reference.Mapping;

internal static class UserProfileMapper
{
    public static UserProfileDto ToDto(ReferenceUserProfile profile)
        => new()
        {
            UserKey = profile.UserKey.ToString(),
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.Email,
            Phone = profile.Phone,
            Status = profile.Status,
            Metadata = profile.Metadata
        };
}

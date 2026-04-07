using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal static class UserProfileMapper
{
    public static UserView ToDto(UserProfile profile)
        => new()
        {
            UserKey = profile.UserKey,
            ProfileKey = profile.ProfileKey,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            BirthDate = profile.BirthDate,
            CreatedAt = profile.CreatedAt,
            Gender = profile.Gender,
            Culture = profile.Culture,
            Language = profile.Language,
            TimeZone = profile.TimeZone,
            Metadata = profile.Metadata
        };
}

using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal static class UserProfileMapper
{
    public static UserViewDto ToDto(UserProfile profile)
        => new()
        {
            UserKey = profile.UserKey.ToString(),
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            DisplayName = profile.DisplayName,
            Bio = profile.Bio,
            BirthDate = profile.BirthDate,
            CreatedAt = profile.CreatedAt,
            Gender = profile.Gender,
            Metadata = profile.Metadata
        };
}

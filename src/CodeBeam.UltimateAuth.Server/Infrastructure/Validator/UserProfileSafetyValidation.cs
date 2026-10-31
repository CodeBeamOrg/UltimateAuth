using System.Text;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public static class UserProfileSafetyValidation
{
    public static IReadOnlyList<UAuthValidationError> Validate(UserProfileInfo profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var errors = new List<UAuthValidationError>();

        Check(profile.FirstName, UserProfileLimits.FirstName, "first_name");
        Check(profile.LastName, UserProfileLimits.LastName, "last_name");
        Check(profile.DisplayName, UserProfileLimits.DisplayName, "display_name");
        Check(profile.Gender, UserProfileLimits.Gender, "gender");
        Check(profile.Bio, UserProfileLimits.Bio, "bio");
        Check(profile.Language, UserProfileLimits.Language, "language");
        Check(profile.TimeZone, UserProfileLimits.TimeZone, "time_zone");
        Check(profile.Culture, UserProfileLimits.Culture, "culture");

        if (profile.Metadata is { } metadata)
        {
            if (metadata.Count > UserProfileLimits.MetadataEntries)
            {
                errors.Add(new UAuthValidationError("profile_metadata_entries_exceeded", "metadata"));
            }

            long totalBytes = 0;

            foreach (var (key, value) in metadata)
            {
                if (key.Length > UserProfileLimits.MetadataKey)
                {
                    errors.Add(new UAuthValidationError("profile_metadata_key_too_long" ,"metadata"));
                }

                if (value is null || value.Length > UserProfileLimits.MetadataValue)
                {
                    errors.Add(new UAuthValidationError("profile_metadata_value_too_long", "metadata"));
                }

                totalBytes += Encoding.UTF8.GetByteCount(key);

                if (value is not null)
                    totalBytes += Encoding.UTF8.GetByteCount(value);
            }

            if (totalBytes > UserProfileLimits.MetadataTotalUtf8Bytes)
            {
                errors.Add(new UAuthValidationError("profile_metadata_too_large", "metadata"));
            }
        }

        return errors;

        void Check(string? value, int maxLength, string field)
        {
            if (value is not null && value.Length > maxLength)
            {
                errors.Add(new UAuthValidationError("profile_field_too_long", field));
            }
        }
    }

    public static void EnsureValid(UserProfileInfo profile)
    {
        var errors = Validate(profile);

        if (errors.Count > 0)
        {
            throw new UAuthValidationException(string.Join(", ", errors));
        }
    }
}

namespace CodeBeam.UltimateAuth.Users.Contracts;

/// <summary>
/// Framework-level safety limits for user profile content.
/// Applications may enforce stricter limits but cannot increase these limits.
/// </summary>
public static class UserProfileLimits
{
    public const int FirstName = 128;
    public const int LastName = 128;
    public const int DisplayName = 256;

    public const int Gender = 128;
    public const int Bio = 4096;

    public const int Language = 64;
    public const int TimeZone = 128;
    public const int Culture = 64;

    public const int MetadataEntries = 32;
    public const int MetadataKey = 128;
    public const int MetadataValue = 2048;
    public const int MetadataTotalUtf8Bytes = 8192;
}

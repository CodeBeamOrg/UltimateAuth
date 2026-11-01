namespace CodeBeam.UltimateAuth.Credentials.Options;

public static class PasswordPolicyLimits
{
    public const int MinimumLength = 6;
    public const int AbsoluteMaxLength = 256;

    public const int DefaultMaxLength = 128;

    public const int MaxHistoryCount = 10;
}

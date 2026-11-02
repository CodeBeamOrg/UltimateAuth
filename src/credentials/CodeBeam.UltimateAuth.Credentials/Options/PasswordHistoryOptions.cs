namespace CodeBeam.UltimateAuth.Credentials.Options;

public sealed class PasswordHistoryOptions
{
    /// <summary>
    /// Number of previous passwords that cannot be reused.
    /// Zero disables password history enforcement.
    /// </summary>
    public int RememberLast { get; set; } = 0;

    public PasswordHistoryOptions Clone() => new()
    {
        RememberLast = RememberLast
    };
}

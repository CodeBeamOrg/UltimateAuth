namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthResetOptions
{
    public TimeSpan TokenValidity { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan CodeValidity { get; set; } = TimeSpan.FromMinutes(10);
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the length for numeric reset codes. Does not affect token-based resets.
    /// Default is 6, which means the code will be a 6-digit number.
    /// </summary>
    public int CodeLength { get; set; } = 6;

    internal UAuthResetOptions Clone() => new()
    {
        TokenValidity = TokenValidity,
        CodeValidity = CodeValidity,
        MaxAttempts = MaxAttempts,
        CodeLength = CodeLength
    };
}

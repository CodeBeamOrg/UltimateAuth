namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthJwtValidationOptions
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    /// <summary>
    /// Allowed signing algorithms. Must match trusted key policy.
    /// </summary>
    public required IReadOnlyCollection<string> ValidAlgorithms { get; init; }

    public TimeSpan ClockSkew { get; init; } = TimeSpan.FromSeconds(30);

    public int MaximumTokenSizeInBytes { get; init; } = 16 * 1024;
}

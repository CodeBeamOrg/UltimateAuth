using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserIdentifierAvailabilityResult
{
    public required bool IsValid { get; init; }

    public required bool IsAvailable { get; init; }

    public string? NormalizedValue { get; init; }

    public IReadOnlyList<UAuthValidationError> Errors { get; init; } = Array.Empty<UAuthValidationError>();

    public static UserIdentifierAvailabilityResult Available(string normalizedValue)
        => new()
        {
            IsValid = true,
            IsAvailable = true,
            NormalizedValue = normalizedValue
        };

    public static UserIdentifierAvailabilityResult Unavailable(string normalizedValue)
        => new()
        {
            IsValid = true,
            IsAvailable = false,
            NormalizedValue = normalizedValue
        };

    public static UserIdentifierAvailabilityResult Invalid(IEnumerable<UAuthValidationError> errors, string? normalizedValue = null)
        => new()
        {
            IsValid = false,
            IsAvailable = false,
            NormalizedValue = normalizedValue,
            Errors = errors.ToArray()
        };
}

using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class UserProfileValidationResult
{
    public bool IsValid { get; }

    public IReadOnlyList<UAuthValidationError> Errors { get; }

    private UserProfileValidationResult(bool isValid, IReadOnlyList<UAuthValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static UserProfileValidationResult Success()
        => new(true, Array.Empty<UAuthValidationError>());

    public static UserProfileValidationResult Failed(IEnumerable<UAuthValidationError> errors)
        => new(false, errors.ToList());
}

using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class UserIdentifierValidationResult
{
    public bool IsValid { get; }

    public IReadOnlyList<UAuthValidationError> Errors { get; }

    private UserIdentifierValidationResult(bool isValid, IReadOnlyList<UAuthValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static UserIdentifierValidationResult Success()
        => new(true, Array.Empty<UAuthValidationError>());

    public static UserIdentifierValidationResult Failed(IEnumerable<UAuthValidationError> errors)
        => new(false, errors.ToList());
}

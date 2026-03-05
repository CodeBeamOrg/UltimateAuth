using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class UserCreateValidatorResult
{
    public bool IsValid { get; }

    public IReadOnlyList<UAuthValidationError> Errors { get; }

    private UserCreateValidatorResult(bool isValid, IReadOnlyList<UAuthValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static UserCreateValidatorResult Success()
        => new(true, Array.Empty<UAuthValidationError>());

    public static UserCreateValidatorResult Failed(IEnumerable<UAuthValidationError> errors)
        => new(false, errors.ToList().AsReadOnly());
}

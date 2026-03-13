using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class IdentifierValidationResult
{
    public bool IsValid { get; }

    public IReadOnlyList<UAuthValidationError> Errors { get; }

    private IdentifierValidationResult(bool isValid, IReadOnlyList<UAuthValidationError> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public static IdentifierValidationResult Success()
        => new(true, Array.Empty<UAuthValidationError>());

    public static IdentifierValidationResult Failed(IEnumerable<UAuthValidationError> errors)
        => new(false, errors.ToList());
}

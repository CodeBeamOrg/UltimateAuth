using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UserCreateValidator : IUserCreateValidator
{
    private readonly IIdentifierValidator _identifierValidator;

    public UserCreateValidator(IIdentifierValidator identifierValidator)
    {
        _identifierValidator = identifierValidator;
    }

    public async Task<UserCreateValidatorResult> ValidateAsync(AccessContext context, CreateUserRequest request, CancellationToken ct = default)
    {
        var errors = new List<UAuthValidationError>();

        if (string.IsNullOrWhiteSpace(request.UserName) &&
            string.IsNullOrWhiteSpace(request.Email) &&
            string.IsNullOrWhiteSpace(request.Phone))
        {
            errors.Add(new("identifier_required"));
        }

        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierDto()
            {
                Type = UserIdentifierType.Username,
                Value = request.UserName
            }, ct);

            errors.AddRange(r.Errors);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierDto()
            {
                Type = UserIdentifierType.Email,
                Value = request.Email
            }, ct);

            errors.AddRange(r.Errors);
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierDto()
            {
                Type = UserIdentifierType.Phone,
                Value = request.Phone
            }, ct);

            errors.AddRange(r.Errors);
        }

        if (errors.Count == 0)
            return UserCreateValidatorResult.Success();

        return UserCreateValidatorResult.Failed(errors);
    }
}

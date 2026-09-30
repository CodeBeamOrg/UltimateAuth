using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UserCreateValidator : IUserCreateValidator
{
    private readonly IUserIdentifierValidator _identifierValidator;
    private readonly IUserProfileValidator _profileValidator;

    public UserCreateValidator(IUserIdentifierValidator identifierValidator, IUserProfileValidator profileValidator)
    {
        _identifierValidator = identifierValidator;
        _profileValidator = profileValidator;
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
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierInfo()
            {
                Type = UserIdentifierType.Username,
                Value = request.UserName
            }, ct);

            errors.AddRange(r.Errors);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierInfo()
            {
                Type = UserIdentifierType.Email,
                Value = request.Email
            }, ct);

            errors.AddRange(r.Errors);
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierInfo()
            {
                Type = UserIdentifierType.Phone,
                Value = request.Phone
            }, ct);

            errors.AddRange(r.Errors);
        }

        var effectiveDisplayName =
            request.DisplayName
            ?? request.UserName
            ?? request.Email
            ?? request.Phone;

        var profileValidation = await _profileValidator.ValidateAsync(context,
            new UserProfileInfo
            {
                ProfileKey = ProfileKey.Default,
                FirstName = request.FirstName,
                LastName = request.LastName,
                DisplayName = effectiveDisplayName,
                BirthDate = request.BirthDate,
                Gender = request.Gender,
                Bio = request.Bio,
                Language = request.Language,
                TimeZone = request.TimeZone,
                Culture = request.Culture
            },
            ct);

        errors.AddRange(profileValidation.Errors);

        if (errors.Count == 0)
            return UserCreateValidatorResult.Success();

        return UserCreateValidatorResult.Failed(errors);
    }
}

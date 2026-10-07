using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UserCreateValidator : IUserCreateValidator
{
    private readonly IUserIdentifierValidator _identifierValidator;
    private readonly IUserIdentifierAvailabilityService _identifierAvailability;
    private readonly IUserProfileValidator _profileValidator;

    public UserCreateValidator(IUserIdentifierValidator identifierValidator, IUserIdentifierAvailabilityService identifierAvailability, IUserProfileValidator profileValidator)
    {
        _identifierValidator = identifierValidator;
        _identifierAvailability = identifierAvailability;
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

            if (r.IsValid)
            {
                var availability = await _identifierAvailability.CheckAsync(
                        context,
                        new CheckUserIdentifierAvailabilityRequest
                        {
                            Type = UserIdentifierType.Username,
                            Value = request.UserName
                        },
                        ct);

                if (!availability.IsAvailable)
                {
                    errors.Add(new UAuthValidationError("username_unavailable", "username"));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierInfo()
            {
                Type = UserIdentifierType.Email,
                Value = request.Email
            }, ct);

            errors.AddRange(r.Errors);

            if (r.IsValid)
            {
                var availability = await _identifierAvailability.CheckAsync(
                        context,
                        new CheckUserIdentifierAvailabilityRequest
                        {
                            Type = UserIdentifierType.Email,
                            Value = request.Email
                        },
                        ct);

                if (!availability.IsAvailable)
                {
                    errors.Add(new UAuthValidationError("email_unavailable", "email"));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var r = await _identifierValidator.ValidateAsync(context, new UserIdentifierInfo()
            {
                Type = UserIdentifierType.Phone,
                Value = request.Phone
            }, ct);

            errors.AddRange(r.Errors);

            if (r.IsValid)
            {
                // TODO: CheckAsync also validates identifiers, make them effective.
                // TODO: This guard doesn't work with concurrent requests.
                var availability = await _identifierAvailability.CheckAsync(context,
                        new CheckUserIdentifierAvailabilityRequest
                        {
                            Type = UserIdentifierType.Phone,
                            Value = request.Phone
                        },
                        ct);

                if (!availability.IsAvailable)
                {
                    errors.Add(
                        new UAuthValidationError("phone_unavailable", "phone"));
                }
            }
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

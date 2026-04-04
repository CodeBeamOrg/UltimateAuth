using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class IdentifierValidator : IIdentifierValidator
{
    private readonly UAuthIdentifierValidationOptions _options;

    public IdentifierValidator(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.IdentifierValidation;
    }

    public Task<IdentifierValidationResult> ValidateAsync(AccessContext context, UserIdentifierInfo identifier, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var errors = new List<UAuthValidationError>();

        if (string.IsNullOrWhiteSpace(identifier.Value))
        {
            errors.Add(new("identifier_empty"));
            return Task.FromResult(IdentifierValidationResult.Failed(errors));
        }

        identifier.Value = identifier.Value.Trim();

        switch (identifier.Type)
        {
            case UserIdentifierType.Username:
                ValidateUsername(identifier.Value, errors);
                break;

            case UserIdentifierType.Email:
                ValidateEmail(identifier.Value, errors);
                break;

            case UserIdentifierType.Phone:
                ValidatePhone(identifier.Value, errors);
                break;
        }

        if (errors.Count == 0)
            return Task.FromResult(IdentifierValidationResult.Success());

        return Task.FromResult(IdentifierValidationResult.Failed(errors));
    }

    private void ValidateUsername(string username, List<UAuthValidationError> errors)
    {
        var rule = _options.UserName;

        if (!rule.Enabled)
            return;

        if (username.Length < rule.MinLength)
            errors.Add(new("username_too_short", "username"));

        if (username.Length > rule.MaxLength)
            errors.Add(new("username_too_long", "username"));

        if (!string.IsNullOrWhiteSpace(rule.AllowedRegex))
        {
            if (!Regex.IsMatch(username, rule.AllowedRegex))
                errors.Add(new("username_invalid_format", "username"));
        }
    }

    private void ValidateEmail(string email, List<UAuthValidationError> errors)
    {
        var rule = _options.Email;

        if (!rule.Enabled)
            return;

        if (email.Length < rule.MinLength)
            errors.Add(new("email_too_short", "email"));

        if (email.Length > rule.MaxLength)
            errors.Add(new("email_too_long", "email"));

        if (!email.Contains('@'))
            errors.Add(new("email_invalid_format", "email"));
    }

    private void ValidatePhone(string phone, List<UAuthValidationError> errors)
    {
        var rule = _options.Phone;

        if (!rule.Enabled)
            return;

        if (phone.Length < rule.MinLength)
            errors.Add(new("phone_too_short", "phone"));

        if (phone.Length > rule.MaxLength)
            errors.Add(new("phone_too_long", "phone"));
    }
}

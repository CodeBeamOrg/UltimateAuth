using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

internal sealed class UAuthConcurrencyLimitingValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var concurrency = options.ConcurrencyLimiting;

        if (concurrency is null)
            return ValidateOptionsResult.Fail("ConcurrencyLimiting is required.");

        if (!concurrency.Enabled)
            return ValidateOptionsResult.Success;

        var errors = new List<string>();

        ValidateRule("PasswordHashing", concurrency.PasswordHashing, errors);
        ValidateRule("Login", concurrency.Login, errors);
        ValidateRule("CredentialManagement", concurrency.CredentialManagement, errors);

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    private static void ValidateRule(string name, UAuthConcurrencyRule? rule, List<string> errors)
    {
        var path = $"ConcurrencyLimiting.{name}";

        if (rule is null)
        {
            errors.Add($"{path} is required.");
            return;
        }

        if (rule.PermitLimit <= 0)
            errors.Add($"{path}.PermitLimit must be positive.");

        if (rule.QueueLimit is < 0)
            errors.Add($"{path}.QueueLimit cannot be negative.");

        if (rule.QueueTimeout <= TimeSpan.Zero)
            errors.Add($"{path}.QueueTimeout must be positive.");

        if (rule.QueueTimeout > TimeSpan.FromMinutes(1))
            errors.Add($"{path}.QueueTimeout cannot exceed one minute.");
    }
}

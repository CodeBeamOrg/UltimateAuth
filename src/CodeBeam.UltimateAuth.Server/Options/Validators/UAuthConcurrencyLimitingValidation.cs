using Microsoft.Extensions.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;

internal sealed class UAuthConcurrencyLimitingValidator
    : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(
        string? name,
        UAuthServerOptions options)
    {
        var concurrency = options.ConcurrencyLimiting;

        if (concurrency is null)
            return ValidateOptionsResult.Fail(
                "ConcurrencyLimiting is required.");

        if (!concurrency.Enabled)
            return ValidateOptionsResult.Success;

        var rule = concurrency.PasswordHashing;

        if (rule is null)
            return ValidateOptionsResult.Fail(
                "ConcurrencyLimiting.PasswordHashing is required.");

        if (rule.PermitLimit <= 0 ||
            rule.PermitLimit > int.MaxValue / 5)
            return ValidateOptionsResult.Fail(
                "PasswordHashing.PermitLimit is outside the supported range.");

        if (rule.QueueLimit is < 0)
            return ValidateOptionsResult.Fail(
                "PasswordHashing.QueueLimit cannot be negative.");

        if (rule.QueueTimeout <= TimeSpan.Zero ||
            rule.QueueTimeout > TimeSpan.FromMinutes(1))
            return ValidateOptionsResult.Fail(
                "PasswordHashing.QueueTimeout must be greater than zero " +
                "and no more than one minute.");

        return ValidateOptionsResult.Success;
    }
}
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options.Validators;

internal sealed class UAuthDeviceLimitOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var limits = options.DeviceLimits;

        if (limits is null)
            return ValidateOptionsResult.Fail("DeviceLimits is required.");

        var errors = new List<string>();

        if (!Enum.IsDefined(limits.Behavior))
        {
            errors.Add("DeviceLimits.Behavior has an invalid value.");
        }

        if (limits.MaxDevices is <= 0)
        {
            errors.Add("DeviceLimits.MaxDevices must be positive when specified.");
        }

        if (limits.TypeLimits is null)
        {
            errors.Add("DeviceLimits.TypeLimits is required.");
        }
        else
        {
            var normalizedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (type, limit) in limits.TypeLimits)
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    errors.Add("DeviceLimits.TypeLimits cannot contain an empty device type.");
                    continue;
                }

                var normalized = type.Trim();

                if (!string.Equals(type, normalized, StringComparison.Ordinal))
                {
                    errors.Add($"DeviceLimits.TypeLimits key '{type}' must not contain surrounding whitespace.");
                }

                if (!normalizedTypes.Add(normalized))
                {
                    errors.Add($"DeviceLimits.TypeLimits contains duplicate device type '{normalized}'.");
                }

                if (limit <= 0)
                {
                    errors.Add($"DeviceLimits.TypeLimits['{type}'] must be positive.");
                }
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

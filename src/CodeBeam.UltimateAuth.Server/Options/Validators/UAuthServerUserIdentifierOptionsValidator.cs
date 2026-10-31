using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerUserIdentifierOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();

        if (!options.Identifiers.Behavior.AllowAdminOverride && !options.Identifiers.Behavior.AllowUserOverride)
        {
            errors.Add("Both AllowAdminOverride and AllowUserOverride cannot be false. " +
                "At least one actor must be able to manage user identifiers.");
        }

        var behavior = options.Identifiers.Behavior;

        if (behavior.MaxIdentifiersPerUser < 1)
        {
            errors.Add("Identifiers.Behavior.MaxIdentifiersPerUser must be greater than zero.");
        }

        if (behavior.MaxIdentifiersPerType is null)
        {
            errors.Add("Identifiers.Behavior.MaxIdentifiersPerType cannot be null.");
        }
        else
        {
            foreach (var (type, limit) in behavior.MaxIdentifiersPerType)
            {
                if (!Enum.IsDefined(type) || limit < 1)
                {
                    errors.Add($"Invalid identifier limit for {type}. Limit must be greater than zero.");
                }
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerUserIdentifierOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        if (!options.Identifiers.AllowAdminOverride && !options.Identifiers.AllowUserOverride)
        {
            return ValidateOptionsResult.Fail("Both AllowAdminOverride and AllowUserOverride cannot be false. " +
                "At least one actor must be able to manage user identifiers.");
        }

        return ValidateOptionsResult.Success;
    }
}

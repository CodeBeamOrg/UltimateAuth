using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

internal sealed class UAuthServerPkceOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var errors = new List<string>();

        if (options.Pkce.AuthorizationCodeLifetimeSeconds <= 0)
        {
            errors.Add("Pkce.AuthorizationCodeLifetimeSeconds must be > 0.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

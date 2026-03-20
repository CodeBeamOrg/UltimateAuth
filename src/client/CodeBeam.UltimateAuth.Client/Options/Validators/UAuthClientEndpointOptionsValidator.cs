using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientEndpointOptionsValidator : IValidateOptions<UAuthClientOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthClientOptions options)
    {
        var e = options.Endpoints;

        if (string.IsNullOrWhiteSpace(e.BasePath))
        {
            return ValidateOptionsResult.Fail("Endpoints.BasePath must be specified.");
        }

        if (string.IsNullOrWhiteSpace(e.Login) ||
            string.IsNullOrWhiteSpace(e.Logout) ||
            string.IsNullOrWhiteSpace(e.Refresh) ||
            string.IsNullOrWhiteSpace(e.Validate))
        {
            return ValidateOptionsResult.Fail("One or more required endpoint paths are missing in UAuthClientEndpointOptions.");
        }

        return ValidateOptionsResult.Success;
    }
}

using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Validates the <see cref="UAuthClientOptions"/> to ensure that all required endpoint paths are specified and not empty.
/// </summary>
public sealed class UAuthClientEndpointOptionsValidator : IValidateOptions<UAuthClientOptions>
{
    /// <summary>
    /// Validates the specified <see cref="UAuthClientOptions"/> instance.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <returns></returns>
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

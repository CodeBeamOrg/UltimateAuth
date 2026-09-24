using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

/// <summary>
/// Validates the <see cref="UAuthClientOptions"/> to ensure that the configuration is consistent and valid.
/// </summary>
public sealed class UAuthClientOptionsValidator : IValidateOptions<UAuthClientOptions>
{
    /// <summary>
    /// Validates the provided <see cref="UAuthClientOptions"/> instance.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValidateOptionsResult Validate(string? name, UAuthClientOptions options)
    {
        if (options.ClientProfile == UAuthClientProfile.NotSpecified && options.AutoDetectClientProfile == false)
        {
            return ValidateOptionsResult.Fail("ClientProfile is NotSpecified while AutoDetectClientProfile is disabled. " +
                "Either specify a ClientProfile or enable auto-detection.");
        }

        return ValidateOptionsResult.Success;
    }
}

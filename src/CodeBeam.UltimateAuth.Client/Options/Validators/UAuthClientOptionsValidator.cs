using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientOptionsValidator : IValidateOptions<UAuthClientOptions>
{
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

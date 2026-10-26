using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientResourceOptionsValidator : IValidateOptions<UAuthClientOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthClientOptions options)
    {
        var resource = options.Resource;

        if (resource is null)
        {
            return ValidateOptionsResult.Fail("Resource options cannot be null.");
        }

        if (!Enum.IsDefined(resource.Credentials))
        {
            return ValidateOptionsResult.Fail("Invalid Resource credentials mode.");
        }

        if (string.IsNullOrWhiteSpace(resource.BaseAddress))
        {
            return ValidateOptionsResult.Success;
        }

        if (!Uri.TryCreate(resource.BaseAddress, UriKind.Absolute, out var uri))
        {
            return ValidateOptionsResult.Fail("Resource BaseAddress must be an absolute URL.");
        }

        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)
        {
            return ValidateOptionsResult.Fail("Resource BaseAddress must use HTTP or HTTPS.");
        }

        if (!string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            return ValidateOptionsResult.Fail("Resource BaseAddress cannot contain credentials, query or fragment.");
        }

        if (uri.Scheme == Uri.UriSchemeHttp && !uri.IsLoopback)
        {
            return ValidateOptionsResult.Fail("Resource BaseAddress must use HTTPS except for loopback development addresses.");
        }

        if (uri.AbsolutePath != "/")
        {
            return ValidateOptionsResult.Fail("Resource BaseAddress must contain only the API origin.");
        }

        return ValidateOptionsResult.Success;
    }
}

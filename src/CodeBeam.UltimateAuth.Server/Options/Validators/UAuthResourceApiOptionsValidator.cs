using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthResourceApiOptionsValidator : IValidateOptions<UAuthResourceApiOptions>
{
    public ValidateOptionsResult Validate(string? name, UAuthResourceApiOptions options)
    {
        var errors = new List<string>();

        var auth = options.Auth;

        if (options is null || auth is null)
            return ValidateOptionsResult.Fail("ResourceApi Auth options cannot be null.");

        if (options.Hub is null)
            errors.Add("Hub must be configured.");

        if (options.Cors is null)
            errors.Add("Cors must be configured.");

        if (options.MultiTenant is null)
            errors.Add("MultiTenant must be configured.");

        if (errors.Count > 0)
            return ValidateOptionsResult.Fail(errors);

        if (!Enum.IsDefined(auth.Mode))
            errors.Add("Auth.Mode is invalid.");

        if (auth.Session is null)
            errors.Add("Auth.Session must be configured.");

        if (auth.AccessToken is null)
            errors.Add("Auth.AccessToken must be configured.");

        if (auth.Device is null)
            errors.Add("Auth.Device must be configured.");

        if (auth.Session is not null)
        {
            if (!Enum.IsDefined(auth.Session.DeliveryMode))
                errors.Add("Auth.Session.DeliveryMode is invalid.");

            if (string.IsNullOrWhiteSpace(auth.Session.CookieName))
                errors.Add("Auth.Session.CookieName is required.");
        }

        if (auth.AccessToken is not null)
        {
            var token = auth.AccessToken;

            if (!Enum.IsDefined(token.DeliveryMode))
                errors.Add("Auth.AccessToken.DeliveryMode is invalid.");

            if (!Enum.IsDefined(token.HeaderFormat))
                errors.Add("Auth.AccessToken.HeaderFormat is invalid.");

            if (string.IsNullOrWhiteSpace(token.CookieName))
                errors.Add("Auth.AccessToken.CookieName is required.");

            if (string.IsNullOrWhiteSpace(token.HeaderName))
                errors.Add("Auth.AccessToken.HeaderName is required.");
        }

        if (auth.Device is not null)
        {
            if (!Enum.IsDefined(auth.Device.MismatchBehavior))
                errors.Add("Auth.Device.MismatchBehavior is invalid.");

            if (auth.Mode == UAuthMode.PureJwt &&
                auth.Device.MismatchBehavior ==
                    DeviceMismatchBehavior.AllowAndRebind)
            {
                errors.Add(
                    "PureJwt does not support device rebinding.");
            }
        }

        if (auth.Mode == UAuthMode.PureJwt)
        {
            if (auth.Jwt is null)
            {
                errors.Add("Auth.Jwt is required in PureJwt mode.");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(auth.Jwt.Issuer))
                    errors.Add("Auth.Jwt.Issuer is required.");

                if (string.IsNullOrWhiteSpace(auth.Jwt.Audience))
                    errors.Add("Auth.Jwt.Audience is required.");

                if (auth.Jwt.ValidAlgorithms is null ||
                    auth.Jwt.ValidAlgorithms.Count == 0 ||
                    auth.Jwt.ValidAlgorithms.Any(
                        string.IsNullOrWhiteSpace))
                {
                    errors.Add(
                        "Auth.Jwt.ValidAlgorithms must contain valid algorithms.");
                }

                if (auth.Jwt.ClockSkew < TimeSpan.Zero)
                    errors.Add("Auth.Jwt.ClockSkew cannot be negative.");

                if (auth.Jwt.MaximumTokenSizeInBytes <= 0)
                    errors.Add(
                        "Auth.Jwt.MaximumTokenSizeInBytes must be positive.");
            }
        }

        if (auth.Mode != UAuthMode.PureJwt)
        {
            if (string.IsNullOrWhiteSpace(options.Hub?.BaseUrl))
            {
                errors.Add(
                    "Hub.BaseUrl is required for remote session validation.");
            }
            else if (!Uri.TryCreate(
                         options.Hub.BaseUrl,
                         UriKind.Absolute,
                         out var uri) ||
                     (uri.Scheme != Uri.UriSchemeHttps &&
                      uri.Scheme != Uri.UriSchemeHttp))
            {
                errors.Add("Hub.BaseUrl must be an absolute HTTP(S) URL.");
            }
        }

        if (string.IsNullOrWhiteSpace(options?.Cors?.PolicyName))
            errors.Add("Cors.PolicyName is required.");

        if (options?.Cors?.AllowedOrigins is null)
            errors.Add("Cors.AllowedOrigins must not be null.");

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}

using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthServerSessionResolutionOptionsValidator : IValidateOptions<UAuthServerOptions>
{
    private static readonly HashSet<string> KnownResolvers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Bearer",
            "Header",
            "Cookie",
            "Query"
        };

    public ValidateOptionsResult Validate(string? name, UAuthServerOptions options)
    {
        var o = options.SessionResolution;

        if (!o.EnableBearer && !o.EnableHeader && !o.EnableCookie && !o.EnableQuery)
        {
            return ValidateOptionsResult.Fail("At least one session resolver must be enabled (Bearer, Header, Cookie, Query).");
        }

        if (o.Order is null || o.Order.Count == 0)
        {
            return ValidateOptionsResult.Fail("SessionResolution.Order cannot be empty.");
        }

        foreach (var item in o.Order)
        {
            if (!KnownResolvers.Contains(item))
            {
                return ValidateOptionsResult.Fail($"Unknown session resolver '{item}' in SessionResolution.Order.");
            }
        }

        foreach (var item in o.Order)
        {
            if (item.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !o.EnableBearer ||
                item.Equals("Header", StringComparison.OrdinalIgnoreCase) && !o.EnableHeader ||
                item.Equals("Cookie", StringComparison.OrdinalIgnoreCase) && !o.EnableCookie ||
                item.Equals("Query", StringComparison.OrdinalIgnoreCase) && !o.EnableQuery)
            {
                return ValidateOptionsResult.Fail($"Session resolver '{item}' is listed in Order but is not enabled.");
            }
        }

        if (o.EnableHeader && string.IsNullOrWhiteSpace(o.HeaderName))
        {
            return ValidateOptionsResult.Fail("SessionResolution.HeaderName must be specified when header resolver is enabled.");
        }

        if (o.EnableQuery && string.IsNullOrWhiteSpace(o.QueryParameterName))
        {
            return ValidateOptionsResult.Fail("SessionResolution.QueryParameterName must be specified when query resolver is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}

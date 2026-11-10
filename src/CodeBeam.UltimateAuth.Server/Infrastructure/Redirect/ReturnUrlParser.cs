using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class ReturnUrlParser
{
    public static ReturnUrlInfo Parse(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return ReturnUrlInfo.None();

        returnUrl = returnUrl.Trim();

        // Reject ambiguous paths, backslashes and control characters.
        if (returnUrl.StartsWith("//", StringComparison.Ordinal) ||
            returnUrl.Contains('\\') ||
            returnUrl.Any(char.IsControl))
        {
            throw new UAuthValidationException("return_url_invalid");
        }

        if (returnUrl.StartsWith("/", StringComparison.Ordinal) ||
            returnUrl.StartsWith("./", StringComparison.Ordinal) ||
            returnUrl.StartsWith("../", StringComparison.Ordinal))
        {
            return ReturnUrlInfo.Relative(returnUrl);
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absolute) &&
            (absolute.Scheme == Uri.UriSchemeHttp ||
             absolute.Scheme == Uri.UriSchemeHttps))
        {
            return ReturnUrlInfo.Absolute(absolute);
        }

        throw new UAuthValidationException("return_url_invalid");
    }
}

using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class ReturnUrlParser
{
    public static ReturnUrlInfo Parse(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return ReturnUrlInfo.None();

        returnUrl = returnUrl.Trim();

        if (returnUrl.StartsWith("/", StringComparison.Ordinal) ||
            returnUrl.StartsWith("./", StringComparison.Ordinal) ||
            returnUrl.StartsWith("../", StringComparison.Ordinal))
        {
            return ReturnUrlInfo.Relative(returnUrl);
        }

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var abs) && (abs.Scheme == Uri.UriSchemeHttp || abs.Scheme == Uri.UriSchemeHttps))
        {
            return ReturnUrlInfo.Absolute(abs);
        }

        if (returnUrl.StartsWith("//", StringComparison.Ordinal))
            throw new UAuthValidationException("return_url_invalid");

        throw new UAuthValidationException("return_url_invalid");
    }
}

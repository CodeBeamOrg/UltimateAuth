namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class ReturnUrlParser
{
    public static ReturnUrlInfo Parse(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return ReturnUrlInfo.None();

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var abs))
            return ReturnUrlInfo.Absolute(abs);

        if (returnUrl.StartsWith("//", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid returnUrl.");

        if (returnUrl.StartsWith("/", StringComparison.Ordinal))
            return ReturnUrlInfo.Relative(returnUrl);

        throw new InvalidOperationException("Invalid returnUrl.");
    }
}

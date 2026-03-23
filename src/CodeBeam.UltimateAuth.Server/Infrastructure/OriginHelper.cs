namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal static class OriginHelper
{
    public static string Normalize(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return string.Empty;

        return origin.Trim().TrimEnd('/').ToLowerInvariant();
    }
}

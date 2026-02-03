namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal static class UAuthUrlBuilder
{
    public static string Combine(string authority, string relative)
    {
        return authority.TrimEnd('/') + "/" + relative.TrimStart('/');
    }
}

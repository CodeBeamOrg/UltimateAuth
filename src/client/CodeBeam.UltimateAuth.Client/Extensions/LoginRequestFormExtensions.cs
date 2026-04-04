using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Extensions;

internal static class LoginRequestFormExtensions
{
    public static IDictionary<string, string> ToDictionary(this LoginRequest request)
        => new Dictionary<string, string>
        {
            ["Identifier"] = request.Identifier,
            ["Secret"] = request.Secret
        };
}

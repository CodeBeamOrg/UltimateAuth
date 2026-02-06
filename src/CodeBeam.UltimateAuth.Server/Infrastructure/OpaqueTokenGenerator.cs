using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    private readonly UAuthTokenOptions _options;

    public OpaqueTokenGenerator(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.Tokens;
    }

    public string Generate() => GenerateBytes(_options.OpaqueIdBytes);
    public string GenerateJwtId() => GenerateBytes(16);
    private static string GenerateBytes(int bytes) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes));
}

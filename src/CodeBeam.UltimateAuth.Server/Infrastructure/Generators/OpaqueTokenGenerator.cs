using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    private readonly UAuthTokenOptions _options;

    public OpaqueTokenGenerator(IOptions<UAuthServerOptions> options)
    {
        _options = options.Value.Token;
    }

    public string Generate() => GenerateBytes(_options.OpaqueIdBytes);
    public string GenerateJwtId() => GenerateBytes(16);
    private static string GenerateBytes(int bytes) => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(bytes));
}

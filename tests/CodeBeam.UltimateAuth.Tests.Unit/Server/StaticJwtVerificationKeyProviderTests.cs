using AwesomeAssertions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class StaticJwtVerificationKeyProviderTests
{
    [Fact]
    public void Multiple_rotation_keys_are_available()
    {
        var oldKey = CreateKey("old-key");
        var currentKey = CreateKey("current-key");

        var provider = new StaticJwtVerificationKeyProvider(
            new[] { oldKey, currentKey });

        provider.GetVerificationKeys()
            .Select(x => x.KeyId)
            .Should()
            .BeEquivalentTo("old-key", "current-key");
    }

    [Fact]
    public void Duplicate_key_ids_are_rejected()
    {
        var keys = new[]
        {
            CreateKey("same-key"),
            CreateKey("same-key")
        };

        var act = () =>
            new StaticJwtVerificationKeyProvider(keys);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Empty_key_set_is_rejected()
    {
        var act = () =>
            new StaticJwtVerificationKeyProvider(
                Array.Empty<SecurityKey>());

        act.Should().Throw<ArgumentException>();
    }

    private static SecurityKey CreateKey(string id)
    {
        return new SymmetricSecurityKey(
            RandomNumberGenerator.GetBytes(32))
        {
            KeyId = id
        };
    }
}

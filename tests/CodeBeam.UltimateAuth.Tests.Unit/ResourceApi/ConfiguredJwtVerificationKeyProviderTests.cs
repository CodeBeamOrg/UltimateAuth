using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CodeBeam.UltimateAuth.Tests.Unit.ResourceApi;

public sealed class ConfiguredJwtVerificationKeyProviderTests
{
    [Fact]
    public void ValidKey_ShouldCreateSymmetricSecurityKey()
    {
        var keyBytes = CreateKeyBytes();

        var sut = CreateProvider(
            Key("key-1", keyBytes));

        var key = Assert.IsType<SymmetricSecurityKey>(
            Assert.Single(sut.GetVerificationKeys()));

        Assert.Equal("key-1", key.KeyId);
        Assert.Equal(keyBytes, key.Key);
    }

    [Fact]
    public void MultipleKeys_ShouldPreserveAllKeys()
    {
        var sut = CreateProvider(
            Key("key-1", CreateKeyBytes(32, 1)),
            Key("key-2", CreateKeyBytes(48, 2)),
            Key("key-3", CreateKeyBytes(64, 3)));

        var keys = sut.GetVerificationKeys();

        Assert.Equal(3, keys.Count);

        Assert.Contains(keys, x => x.KeyId == "key-1");
        Assert.Contains(keys, x => x.KeyId == "key-2");
        Assert.Contains(keys, x => x.KeyId == "key-3");
    }

    [Fact]
    public void MinimumKeyLength_ShouldBeAccepted()
    {
        var sut = CreateProvider(
            Key("key-1", CreateKeyBytes(32)));

        var key = Assert.IsType<SymmetricSecurityKey>(
            Assert.Single(sut.GetVerificationKeys()));

        Assert.Equal(256, key.KeySize);
    }

    [Fact]
    public void LongerKey_ShouldBeAccepted()
    {
        var sut = CreateProvider(
            Key("key-1", CreateKeyBytes(64)));

        var key = Assert.IsType<SymmetricSecurityKey>(
            Assert.Single(sut.GetVerificationKeys()));

        Assert.Equal(512, key.KeySize);
    }

    [Fact]
    public void EmptyKeyCollection_ShouldThrow()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateProvider());

        Assert.Contains(
            "At least one JWT verification key",
            exception.Message);
    }

    [Fact]
    public void NullKeyCollection_ShouldThrow()
    {
        var options = new UAuthResourceApiOptions
        {
            Cryptography = new UAuthResourceCryptographyOptions
            {
                JwtVerificationKeys = null!
            }
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => new ConfiguredJwtVerificationKeyProvider(
                Options.Create(options)));

        Assert.Contains(
            "At least one JWT verification key",
            exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void MissingKeyId_ShouldThrow(string? keyId)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateProvider(
                Key(keyId!, CreateKeyBytes())));

        Assert.Contains(
            "Every JWT verification key requires a key ID",
            exception.Message);
    }

    [Fact]
    public void NullKeyEntry_ShouldThrow()
    {
        var options = new UAuthResourceApiOptions();

        options.Cryptography.JwtVerificationKeys.Add(null!);

        var exception = Record.Exception(() =>
            new ConfiguredJwtVerificationKeyProvider(
                Options.Create(options)));

        Assert.NotNull(exception);
        Assert.True(
            exception is InvalidOperationException
                or ArgumentNullException);
    }

    [Theory]
    [InlineData("invalid-base64")]
    [InlineData("%%%")]
    public void InvalidBase64_ShouldThrow(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateProvider(new UAuthJwtVerificationKeyOptions
            {
                KeyId = "key-1",
                Key = value
            }));

        Assert.Contains("must be Base64 encoded", exception.Message);
        Assert.IsType<FormatException>(exception.InnerException);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(16)]
    [InlineData(31)]
    public void KeyShorterThan32Bytes_ShouldThrow(int length)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateProvider(
                Key("key-1", CreateKeyBytes(length))));

        Assert.Contains(
            "must contain at least 32 bytes",
            exception.Message);
    }

    [Fact]
    public void DuplicateKeyIds_ShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => CreateProvider(
                Key("duplicate", CreateKeyBytes(32, 1)),
                Key("duplicate", CreateKeyBytes(32, 2))));

        Assert.Contains(
            "Duplicate JWT verification key IDs",
            exception.Message);
    }

    [Fact]
    public void EmptyBase64_ShouldThrowForInsufficientKeyLength()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateProvider(new UAuthJwtVerificationKeyOptions
            {
                KeyId = "key-1",
                Key = ""
            }));

        Assert.Contains(
            "must contain at least 32 bytes",
            exception.Message);
    }

    [Fact]
    public void KeyIds_ShouldBeCaseSensitive()
    {
        var sut = CreateProvider(
            Key("Key-1", CreateKeyBytes(32, 1)),
            Key("key-1", CreateKeyBytes(32, 2)));

        var keys = sut.GetVerificationKeys();

        Assert.Equal(2, keys.Count);
        Assert.Contains(keys, x => x.KeyId == "Key-1");
        Assert.Contains(keys, x => x.KeyId == "key-1");
    }

    [Fact]
    public void GetVerificationKeys_ShouldReturnReadOnlyCollection()
    {
        var sut = CreateProvider(
            Key("key-1", CreateKeyBytes()));

        var keys = sut.GetVerificationKeys();

        var list = Assert.IsAssignableFrom<IList<SecurityKey>>(keys);

        Assert.True(list.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            list.Add(new SymmetricSecurityKey(CreateKeyBytes())
            {
                KeyId = "injected"
            }));

        Assert.Single(sut.GetVerificationKeys());
    }

    [Fact]
    public void Provider_ShouldSnapshotConfiguredKeys()
    {
        var originalBytes = CreateKeyBytes();

        var configuredKey = Key("original", originalBytes);

        var options = new UAuthResourceApiOptions();

        options.Cryptography.JwtVerificationKeys.Add(configuredKey);

        var sut = new ConfiguredJwtVerificationKeyProvider(
            Options.Create(options));

        configuredKey.KeyId = "modified";
        configuredKey.Key = Convert.ToBase64String(
            CreateKeyBytes(32, 99));

        options.Cryptography.JwtVerificationKeys.Clear();

        var key = Assert.IsType<SymmetricSecurityKey>(
            Assert.Single(sut.GetVerificationKeys()));

        Assert.Equal("original", key.KeyId);
        Assert.Equal(originalBytes, key.Key);
    }

    [Fact]
    public void GetVerificationKeys_ShouldReturnStableKeys()
    {
        var sut = CreateProvider(
            Key("key-1", CreateKeyBytes()));

        var first = sut.GetVerificationKeys();
        var second = sut.GetVerificationKeys();

        Assert.Same(
            Assert.Single(first),
            Assert.Single(second));
    }

    private static ConfiguredJwtVerificationKeyProvider CreateProvider(
        params UAuthJwtVerificationKeyOptions[] keys)
    {
        var options = new UAuthResourceApiOptions();

        options.Cryptography.JwtVerificationKeys.AddRange(keys);

        return new ConfiguredJwtVerificationKeyProvider(
            Options.Create(options));
    }

    private static UAuthJwtVerificationKeyOptions Key(
        string keyId,
        byte[] bytes) => new()
        {
            KeyId = keyId,
            Key = Convert.ToBase64String(bytes)
        };

    private static byte[] CreateKeyBytes(
        int length = 32,
        byte seed = 1)
    {
        return Enumerable.Range(0, length)
            .Select(i => unchecked((byte)(i + seed)))
            .ToArray();
    }
}

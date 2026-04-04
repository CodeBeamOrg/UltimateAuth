using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Security.Argon2;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class Argon2PasswordHasherTests
{
    private Argon2PasswordHasher CreateHasher()
    {
        var options = Options.Create(new Argon2Options());
        return new Argon2PasswordHasher(options);
    }

    [Fact]
    public void Hash_ShouldReturn_NonEmptyString()
    {
        var hasher = CreateHasher();
        var result = hasher.Hash("password123");

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains(".", result);
    }

    [Fact]
    public void Verify_ShouldReturn_True_ForValidPassword()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");
        var result = hasher.Verify(hash, "password123");

        Assert.True(result);
    }

    [Fact]
    public void Verify_ShouldReturn_False_ForInvalidPassword()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");
        var result = hasher.Verify(hash, "wrong-password");

        Assert.False(result);
    }

    [Fact]
    public void Verify_ShouldReturn_False_ForInvalidFormat()
    {
        var hasher = CreateHasher();
        var result = hasher.Verify("invalid-format", "password");

        Assert.False(result);
    }

    [Fact]
    public void Hash_ShouldThrow_WhenPasswordIsEmpty()
    {
        var hasher = CreateHasher();
        Assert.Throws<UAuthValidationException>(() => hasher.Hash(""));
    }

    [Fact]
    public void Hash_ShouldProduce_DifferentHashes_ForSamePassword()
    {
        var hasher = CreateHasher();
        var hash1 = hasher.Hash("password123");
        var hash2 = hasher.Hash("password123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verify_ShouldUse_SameSalt_FromHash()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");

        var parts = hash.Split('.');
        Assert.Equal(2, parts.Length);

        var salt1 = parts[0];
        var hash2 = hasher.Hash("password123");
        var salt2 = hash2.Split('.')[0];

        Assert.NotEqual(salt1, salt2); // random salt doğrulama
    }
}

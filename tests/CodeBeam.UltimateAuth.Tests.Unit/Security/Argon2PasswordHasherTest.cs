using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Security.Argon2;
using FluentAssertions;
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
    public void Hash_Should_Return_Valid_PasswordHash()
    {
        var hasher = CreateHasher();

        var result = hasher.Hash("password123");

        result.Should().NotBeNull();
        result.Algorithm.Should().Be(PasswordAlgorithms.Argon2);
        result.Hash.Should().NotBeNullOrWhiteSpace();

        var parts = result.Hash.Split('.');
        parts.Length.Should().Be(5);
    }

    [Fact]
    public void Verify_Should_Return_True_For_Correct_Password()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("password123");

        var result = hasher.Verify(hash, "password123");

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_Should_Return_False_For_Wrong_Password()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("password123");

        var result = hasher.Verify(hash, "wrong");

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_Return_False_For_Invalid_Format()
    {
        var hasher = CreateHasher();

        var invalid = PasswordHash.Create(PasswordAlgorithms.Argon2, "invalid");

        var result = hasher.Verify(invalid, "password");

        result.Should().BeFalse();
    }

    [Fact]
    public void Hash_Should_Throw_When_Password_Is_Empty()
    {
        var hasher = CreateHasher();

        Assert.Throws<UAuthValidationException>(() => hasher.Hash(""));
    }

    [Fact]
    public void Hash_Should_Produce_Different_Hashes_For_Same_Password()
    {
        var hasher = CreateHasher();

        var hash1 = hasher.Hash("password123");
        var hash2 = hasher.Hash("password123");

        hash1.Hash.Should().NotBe(hash2.Hash);
    }

    [Fact]
    public void Verify_Should_Use_Embedded_Salt_And_Parameters()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("password123");

        // parametreleri değiştir (simulate config drift)
        var differentOptions = Options.Create(new Argon2Options
        {
            Iterations = 999,
            MemorySizeKb = 999,
            Parallelism = 1,
            SaltSize = 16,
            HashSize = 32
        });

        var differentHasher = new Argon2PasswordHasher(differentOptions);

        // 🔥 yine de doğrulamalı
        var result = differentHasher.Verify(hash, "password123");

        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_Should_Return_True_When_Parameters_Changed()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("password123");

        var differentOptions = Options.Create(new Argon2Options
        {
            Iterations = 999,
            MemorySizeKb = 999,
            Parallelism = 1,
            SaltSize = 16,
            HashSize = 32
        });

        var differentHasher = new Argon2PasswordHasher(differentOptions);

        var result = differentHasher.NeedsRehash(hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_Should_Return_False_When_Parameters_Match()
    {
        var hasher = CreateHasher();

        var hash = hasher.Hash("password123");

        var result = hasher.NeedsRehash(hash);

        result.Should().BeFalse();
    }
}

using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Security.Argon2;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class Argon2PasswordHasherTests
{
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

        var differentOptions = Options.Create(new Argon2Options
        {
            Iterations = 999,
            MemorySizeKb = 999,
            Parallelism = 1,
            SaltSize = 16,
            HashSize = 32
        });

        var differentHasher = new Argon2PasswordHasher(differentOptions);

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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Hash_Should_Throw_When_Password_Is_Null_Or_Empty(
    string? password)
    {
        var hasher = CreateHasher();
        var act = () => hasher.Hash(password!);

        act.Should().Throw<UAuthValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Verify_Should_Return_False_When_Secret_Is_Invalid(
        string? secret)
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");
        var result = hasher.Verify(hash, secret!);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_Should_Return_False_When_Algorithm_Is_Not_Argon2()
    {
        var hasher = CreateHasher();

        var hash = PasswordHash.Create("different-algorithm", "3.65536.1.c2FsdA==.aGFzaA==");

        var result = hasher.Verify(hash, "password123");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("invalid.65536.1.c2FsdA==.aGFzaA==")]
    [InlineData("3.invalid.1.c2FsdA==.aGFzaA==")]
    [InlineData("3.65536.invalid.c2FsdA==.aGFzaA==")]
    public void Verify_Should_Return_False_When_Parameters_Are_Invalid(string encoded)
    {
        var hasher = CreateHasher();
        var hash = PasswordHash.Create(PasswordAlgorithms.Argon2, encoded);
        var result = hasher.Verify(hash, "password123");

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("3.65536.1.NOT_BASE64.aGFzaA==")]
    [InlineData("3.65536.1.c2FsdA==.NOT_BASE64")]
    public void Verify_Should_Return_False_When_Hash_Contains_Invalid_Base64(
    string encoded)
    {
        var hasher = CreateHasher();
        var hash = PasswordHash.Create(PasswordAlgorithms.Argon2, encoded);
        var result = hasher.Verify(hash, "password123");

        result.Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_Should_Return_True_When_Algorithm_Is_Not_Argon2()
    {
        var hasher = CreateHasher();
        var hash = PasswordHash.Create("different-algorithm", "anything");

        hasher.NeedsRehash(hash).Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("1.2.3")]
    [InlineData("1.2.3.4")]
    [InlineData("1.2.3.4.5.6")]
    public void NeedsRehash_Should_Return_True_When_Format_Is_Invalid(
        string encoded)
    {
        var hasher = CreateHasher();

        var hash = PasswordHash.Create(
            PasswordAlgorithms.Argon2,
            encoded);

        hasher.NeedsRehash(hash)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("invalid.65536.1.c2FsdA==.aGFzaA==")]
    [InlineData("3.invalid.1.c2FsdA==.aGFzaA==")]
    [InlineData("3.65536.invalid.c2FsdA==.aGFzaA==")]
    public void NeedsRehash_Should_Return_True_When_Parameters_Are_Invalid(
        string encoded)
    {
        var hasher = CreateHasher();

        var hash = PasswordHash.Create(
            PasswordAlgorithms.Argon2,
            encoded);

        hasher.NeedsRehash(hash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void NeedsRehash_Should_Return_True_When_Iterations_Changed()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");

        var differentHasher = CreateHasher(new Argon2Options
        {
            Iterations = 4
        });

        differentHasher.NeedsRehash(hash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void NeedsRehash_Should_Return_True_When_Memory_Size_Changed()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");

        var differentHasher = CreateHasher(new Argon2Options
        {
            MemorySizeKb = 32 * 1024
        });

        differentHasher.NeedsRehash(hash)
            .Should()
            .BeTrue();
    }

    [Fact]
    public void NeedsRehash_Should_Return_True_When_Parallelism_Changed()
    {
        var hasher = CreateHasher();
        var hash = hasher.Hash("password123");

        var differentParallelism =
            new Argon2Options().Parallelism == 1
                ? 2
                : 1;

        var differentHasher = CreateHasher(new Argon2Options
        {
            Parallelism = differentParallelism
        });

        differentHasher.NeedsRehash(hash)
            .Should()
            .BeTrue();
    }

    private static Argon2PasswordHasher CreateHasher()
    {
        return CreateHasher(new Argon2Options());
    }

    private static Argon2PasswordHasher CreateHasher(
        Argon2Options options)
    {
        return new Argon2PasswordHasher(
            Options.Create(options));
    }
}

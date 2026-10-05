using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Security.Argon2;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class Argon2ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddUltimateAuthArgon2_ShouldRegisterPasswordHasher()
    {
        var services = new ServiceCollection();
        services.AddUltimateAuthArgon2();

        using var provider = services.BuildServiceProvider();

        var hasher = provider.GetRequiredService<IUAuthPasswordHasher>();

        hasher.Should().BeOfType<Argon2PasswordHasher>();
    }

    [Fact]
    public void AddUltimateAuthArgon2_WithoutConfiguration_ShouldRegisterDefaultOptions()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthArgon2();

        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<Argon2Options>>().Value;

        options.Should().NotBeNull();
        options.Iterations.Should().Be(3);
        options.MemorySizeKb.Should().Be(64 * 1024);
        options.SaltSize.Should().Be(16);
        options.HashSize.Should().Be(32);
    }

    [Fact]
    public void AddUltimateAuthArgon2_WithConfiguration_ShouldApplyConfiguration()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthArgon2(options =>
        {
            options.Iterations = 7;
            options.MemorySizeKb = 32768;
            options.Parallelism = 2;
            options.SaltSize = 24;
            options.HashSize = 48;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<Argon2Options>>().Value;

        options.Iterations.Should().Be(7);
        options.MemorySizeKb.Should().Be(32768);
        options.Parallelism.Should().Be(2);
        options.SaltSize.Should().Be(24);
        options.HashSize.Should().Be(48);
    }

    [Fact]
    public void AddUltimateAuthArgon2_ConfiguredParameters_ShouldBeUsedByHasher()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuthArgon2(options =>
        {
            options.Iterations = 4;
            options.MemorySizeKb = 16384;
            options.Parallelism = 2;
        });

        using var provider = services.BuildServiceProvider();
        var hasher = provider.GetRequiredService<IUAuthPasswordHasher>();
        var hash = hasher.Hash("Password123!");
        var parts = hash.Hash.Split('.');

        parts.Should().HaveCount(5);
        parts[0].Should().Be("4");
        parts[1].Should().Be("16384");
        parts[2].Should().Be("2");
    }
}

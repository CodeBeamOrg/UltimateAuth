using CodeBeam.UltimateAuth.EntityFrameworkCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CodeBeam.UltimateAuth.Tests.Unit.EntityFrameworkCore;

public sealed class UAuthEfCoreOptionsTests
{
    [Fact]
    public void Resolve_WithSpecificConfiguration_ShouldReturnSpecific()
    {
        Action<DbContextOptionsBuilder> defaultConfig = _ => { };
        Action<DbContextOptionsBuilder> specificConfig = _ => { };

        var options = new UAuthEfCoreOptions
        {
            Default = defaultConfig
        };

        var result = options.Resolve(specificConfig);

        result.Should().BeSameAs(specificConfig);
    }

    [Fact]
    public void Resolve_WithoutSpecificConfiguration_ShouldReturnDefault()
    {
        Action<DbContextOptionsBuilder> defaultConfig = _ => { };

        var options = new UAuthEfCoreOptions
        {
            Default = defaultConfig
        };

        var result = options.Resolve(null);

        result.Should().BeSameAs(defaultConfig);
    }

    [Fact]
    public void Resolve_WithoutAnyConfiguration_ShouldThrow()
    {
        var options = new UAuthEfCoreOptions();

        var act = () => options.Resolve(null);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "No database configuration provided for UltimateAuth EFCore.*");
    }
}

using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Core.Runtime;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class CoreProductInfoTests
{
    [Fact]
    public void ProductInfo_should_have_expected_defaults()
    {
        var info = new UAuthProductInfo();

        info.ProductName.Should().Be("UltimateAuth");
        info.RuntimeId.Should().NotBeNullOrWhiteSpace();

        Guid.TryParseExact(info.RuntimeId, "N", out _)
            .Should().BeTrue();
    }

    [Fact]
    public void ProductInfo_should_generate_unique_runtime_ids()
    {
        var first = new UAuthProductInfo();
        var second = new UAuthProductInfo();

        first.RuntimeId.Should().NotBe(second.RuntimeId);
    }

    [Fact]
    public void ProductInfo_should_allow_explicit_initialization()
    {
        var startedAt = DateTimeOffset.UtcNow;

        var info = new UAuthProductInfo
        {
            ProductName = "Custom UltimateAuth",
            Version = "1.2.3",
            InformationalVersion = "1.2.3-preview",
            StartedAt = startedAt,
            RuntimeId = "custom-runtime"
        };

        info.ProductName.Should().Be("Custom UltimateAuth");
        info.Version.Should().Be("1.2.3");
        info.InformationalVersion.Should().Be("1.2.3-preview");
        info.StartedAt.Should().Be(startedAt);
        info.RuntimeId.Should().Be("custom-runtime");
    }

    [Fact]
    public void Provider_should_return_correct_assembly_metadata()
    {
        var provider = CreateProvider();
        var info = provider.Get();

        var assembly = typeof(UAuthProductInfoProvider).Assembly;

        var expectedVersion =
            assembly.GetName().Version?.ToString(3) ?? "unknown";

        var expectedInformationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        info.ProductName.Should().Be("UltimateAuth");
        info.Version.Should().Be(expectedVersion);
        info.InformationalVersion.Should().Be(expectedInformationalVersion);
    }

    [Fact]
    public void Provider_should_capture_creation_time()
    {
        var before = DateTimeOffset.UtcNow;

        var provider = CreateProvider();

        var after = DateTimeOffset.UtcNow;
        var info = provider.Get();

        info.StartedAt.Should().BeOnOrAfter(before);
        info.StartedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public void Provider_should_generate_valid_runtime_id()
    {
        var info = CreateProvider().Get();

        info.RuntimeId.Should().NotBeNullOrWhiteSpace();

        Guid.TryParseExact(info.RuntimeId, "N", out _)
            .Should().BeTrue();
    }

    [Fact]
    public void Provider_should_return_same_info_instance()
    {
        var provider = CreateProvider();

        var first = provider.Get();
        var second = provider.Get();

        first.Should().BeSameAs(second);
        first.RuntimeId.Should().Be(second.RuntimeId);
        first.StartedAt.Should().Be(second.StartedAt);
    }

    [Fact]
    public void Different_providers_should_have_different_runtime_ids()
    {
        var first = CreateProvider().Get();
        var second = CreateProvider().Get();

        first.Should().NotBeSameAs(second);
        first.RuntimeId.Should().NotBe(second.RuntimeId);
    }

    [Fact]
    public void Provider_should_not_depend_on_option_values()
    {
        var options = new UAuthOptions();

        var provider = new UAuthProductInfoProvider(
            Options.Create(options));

        var info = provider.Get();

        info.ProductName.Should().Be("UltimateAuth");
        info.Version.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Core_registration_should_register_product_info_provider_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddUltimateAuth();

        var descriptors = services
            .Where(d => d.ServiceType == typeof(IUAuthProductInfoProvider))
            .ToList();

        descriptors.Should().ContainSingle();

        descriptors[0].ImplementationType
            .Should().Be(typeof(UAuthProductInfoProvider));

        descriptors[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Direct_configuration_marker_should_start_unconfigured()
    {
        var marker = new DirectCoreConfigurationMarker();

        marker.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Direct_configuration_marker_should_become_configured()
    {
        var marker = new DirectCoreConfigurationMarker();

        marker.MarkConfigured();

        marker.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public void Direct_configuration_marker_should_remain_configured_after_multiple_calls()
    {
        var marker = new DirectCoreConfigurationMarker();

        marker.MarkConfigured();
        marker.MarkConfigured();

        marker.IsConfigured.Should().BeTrue();
    }

    private static UAuthProductInfoProvider CreateProvider()
    {
        return new UAuthProductInfoProvider(
            Options.Create(new UAuthOptions()));
    }
}

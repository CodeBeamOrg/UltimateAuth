using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Runtime;
using FluentAssertions;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthServerProductInfoTests
{
    [Fact]
    public void ProductInfo_should_have_expected_defaults()
    {
        var info = new UAuthServerProductInfo();

        info.ProductName.Should().Be("UltimateAuth Server");
        info.RuntimeId.Should().NotBeNullOrWhiteSpace();

        Guid.TryParseExact(info.RuntimeId, "N", out _)
            .Should().BeTrue();
    }

    [Fact]
    public void ProductInfo_should_generate_unique_runtime_ids()
    {
        var first = new UAuthServerProductInfo();
        var second = new UAuthServerProductInfo();

        first.RuntimeId.Should().NotBe(second.RuntimeId);
    }

    [Theory]
    [InlineData(UAuthHubDeploymentMode.Integrated, false)]
    [InlineData(UAuthHubDeploymentMode.Integrated, true)]
    public void Provider_should_reflect_server_options(
        UAuthHubDeploymentMode deploymentMode,
        bool multiTenancyEnabled)
    {
        var options = new UAuthServerOptions
        {
            HubDeploymentMode = deploymentMode
        };

        options.MultiTenant.Enabled = multiTenancyEnabled;

        var provider = new UAuthServerProductInfoProvider(
            Options.Create(options));

        var info = provider.Get();

        info.HubDeploymentMode.Should().Be(deploymentMode);
        info.MultiTenancyEnabled.Should().Be(multiTenancyEnabled);
    }

    [Fact]
    public void Provider_should_return_correct_assembly_metadata()
    {
        var provider = CreateProvider();

        var info = provider.Get();

        var assembly = typeof(UAuthServerProductInfoProvider).Assembly;

        var expectedVersion =
            assembly.GetName().Version?.ToString(3) ?? "unknown";

        var expectedInformationalVersion =
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        info.ProductName.Should().Be("UltimateAuth Server");
        info.Version.Should().Be(expectedVersion);
        info.InformationalVersion.Should().Be(expectedInformationalVersion);
        info.FrameworkDescription.Should().Be(
            RuntimeInformation.FrameworkDescription);
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
    public void Provider_should_snapshot_options_at_construction()
    {
        var options = new UAuthServerOptions();
        options.MultiTenant.Enabled = false;

        var provider = new UAuthServerProductInfoProvider(
            Options.Create(options));

        options.MultiTenant.Enabled = true;

        provider.Get().MultiTenancyEnabled.Should().BeFalse();
    }

    private static UAuthServerProductInfoProvider CreateProvider()
    {
        return new UAuthServerProductInfoProvider(
            Options.Create(new UAuthServerOptions()));
    }
}

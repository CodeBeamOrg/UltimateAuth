using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client;

public class UAuthUrlBuilderTests
{
    [Theory]
    [InlineData("/auth", "/login", "/acme/auth/login")]
    [InlineData("/auth/", "/login", "/acme/auth/login")]
    [InlineData("https://localhost:6110/auth", "/login", "https://localhost:6110/acme/auth/login")]
    [InlineData("https://example.com/api/auth", "/refresh", "https://example.com/acme/api/auth/refresh")]
    [InlineData("https://example.com/auth/", "/pkce/authorize", "https://example.com/acme/auth/pkce/authorize")]
    public void Build_RouteTenant_InsertsTenantBeforeBasePath(string basePath, string endpoint, string expected)
    {
        var tenant = new UAuthClientMultiTenantOptions
        {
            Enabled = true,
            Tenant = "acme",
            Transport = TenantTransport.Route
        };

        var result = UAuthUrlBuilder.Build(
            basePath, endpoint, tenant);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(false, TenantTransport.Route)]
    [InlineData(true, TenantTransport.Header)]
    [InlineData(true, TenantTransport.None)]
    public void Build_WithoutRouteTenant_PreservesUrl(
        bool enabled, TenantTransport transport)
    {
        var tenant = new UAuthClientMultiTenantOptions
        {
            Enabled = enabled,
            Tenant = "acme",
            Transport = transport
        };

        var result = UAuthUrlBuilder.Build(
            "https://example.com/auth", "/login", tenant);

        Assert.Equal("https://example.com/auth/login", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Build_RouteTenantWithoutValue_Throws(string? value)
    {
        var tenant = new UAuthClientMultiTenantOptions
        {
            Enabled = true,
            Tenant = value,
            Transport = TenantTransport.Route
        };

        Assert.Throws<InvalidOperationException>(() =>
            UAuthUrlBuilder.Build("/auth", "/login", tenant));
    }

    [Theory]
    [InlineData("foo/bar")]
    [InlineData("../admin")]
    [InlineData("foo?bar")]
    [InlineData("foo#bar")]
    public void Build_InvalidTenantSegment_Throws(string value)
    {
        var tenant = new UAuthClientMultiTenantOptions
        {
            Enabled = true,
            Tenant = value,
            Transport = TenantTransport.Route
        };

        Assert.Throws<ArgumentException>(() =>
            UAuthUrlBuilder.Build("/auth", "/login", tenant));
    }
}

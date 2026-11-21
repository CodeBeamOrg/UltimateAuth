using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class TransportCredentialResolverTests
{
    [Fact]
    public async Task ResolveAsync_PureOpaque_WithSessionAndAccessCookies_ReturnsSessionCredential()
    {
        var deviceResolver = new Mock<IDeviceResolver>();

        deviceResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(TestDevice.DefaultDeviceInfo());

        using var services = new ServiceCollection()
            .AddSingleton(deviceResolver.Object)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        context.Items[UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(TenantKey.Single);

        context.Request.Headers.Cookie =
            "uas=old-session; uat=valid-jwt";

        var response = new EffectiveAuthResponse(
            new EffectiveGrantDelivery(
                GrantKind.Session,
                TokenFormat.Opaque,
                GrantDeliveryMode.Cookie,
                cookie: new UAuthCookieOptions
                {
                    Name = "uas"
                }),
            EffectiveGrantDelivery.Disabled(GrantKind.AccessToken),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);

        var sut = new TransportCredentialResolver();

        var credential = await sut.ResolveAsync(
            context,
            UAuthMode.PureOpaque,
            response);

        credential.Should().NotBeNull();

        credential!.Kind.Should()
            .Be(TransportCredentialKind.Session);

        credential.Value.Should()
            .Be("old-session");
    }


    [Fact]
    public async Task ResolveAsync_PureJwt_WithStaleSessionCookie_UsesAccessToken()
    {
        var fixture = CreateFixture(
            "uas=old-session; uat=valid-jwt");

        var delivery = CreateResponse(
            accessMode: GrantDeliveryMode.Cookie);

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureJwt,
            delivery);

        credential.Should().NotBeNull();
        credential!.Kind.Should()
            .Be(TransportCredentialKind.AccessToken);
        credential.Value.Should().Be("valid-jwt");
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithOnlySessionCookie_ReturnsNull()
    {
        var fixture = CreateFixture("uas=old-session");

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureJwt,
            CreateResponse(GrantDeliveryMode.Cookie));

        credential.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithRefreshAndAccessCookies_UsesAccessToken()
    {
        var fixture = CreateFixture(
            "uar=old-refresh; uat=valid-jwt");

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureJwt,
            CreateResponse(GrantDeliveryMode.Cookie));

        credential.Should().NotBeNull();
        credential!.Kind.Should()
            .Be(TransportCredentialKind.AccessToken);
        credential.Value.Should().Be("valid-jwt");
    }

    [Theory]
    [InlineData(HeaderTokenFormat.Bearer, "Bearer valid-jwt")]
    [InlineData(HeaderTokenFormat.Raw, "valid-jwt")]
    public async Task ResolveAsync_PureJwt_UsesConfiguredHeader(
        HeaderTokenFormat format,
        string value)
    {
        var fixture = CreateFixture("uas=old-session");
        fixture.Context.Request.Headers["X-UAuth-Access"] = value;

        var response = CreateResponse(
            GrantDeliveryMode.Header,
            headerName: "X-UAuth-Access",
            headerFormat: format);

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureJwt,
            response);

        credential.Should().NotBeNull();
        credential!.Kind.Should()
            .Be(TransportCredentialKind.AccessToken);
        credential.Value.Should().Be("valid-jwt");
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_HeaderDelivery_DoesNotFallBackToCookie()
    {
        var fixture = CreateFixture("uat=valid-jwt");

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureJwt,
            CreateResponse(GrantDeliveryMode.Header));

        credential.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_PureOpaque_UsesSessionCookie()
    {
        var fixture = CreateFixture(
            "uas=valid-session; uat=old-access");

        var credential = await fixture.Sut.ResolveAsync(
            fixture.Context,
            UAuthMode.PureOpaque,
            CreateResponse(GrantDeliveryMode.Cookie));

        credential.Should().NotBeNull();
        credential!.Kind.Should()
            .Be(TransportCredentialKind.Session);
        credential.Value.Should().Be("valid-session");
    }


    private static (
        TransportCredentialResolver Sut,
        DefaultHttpContext Context)
        CreateFixture(string cookieHeader)
    {
        var deviceResolver = new Mock<IDeviceResolver>();

        deviceResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(TestDevice.DefaultDeviceInfo());

        var services = new ServiceCollection()
            .AddSingleton(deviceResolver.Object)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        context.Items[UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(TenantKey.Single);

        context.Request.Headers.Cookie = cookieHeader;

        return (new TransportCredentialResolver(), context);
    }

    private static EffectiveAuthResponse CreateResponse(
        GrantDeliveryMode accessMode,
        string headerName = "uat",
        HeaderTokenFormat headerFormat = HeaderTokenFormat.Bearer)
    {
        return new EffectiveAuthResponse(
            new EffectiveGrantDelivery(
                GrantKind.Session,
                TokenFormat.Opaque,
                GrantDeliveryMode.Cookie,
                cookie: new UAuthCookieOptions { Name = "uas" }),
            new EffectiveGrantDelivery(
                GrantKind.AccessToken,
                TokenFormat.Jwt,
                accessMode,
                headerName,
                headerFormat,
                new UAuthCookieOptions { Name = "uat" }),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);
    }

}

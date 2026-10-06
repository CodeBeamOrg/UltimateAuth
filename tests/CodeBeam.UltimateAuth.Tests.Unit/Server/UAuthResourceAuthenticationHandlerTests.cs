using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Authentication;

public sealed class UAuthResourceAuthenticationHandlerTests
{
    private static readonly TenantKey Tenant = TenantKey.FromExternal("tenant-1");

    private static readonly UserKey User = UserKey.FromString("user-1");

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialDoesNotExist_ShouldReturnNoResult()
    {
        var setup = CreateTestContext();

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>()))
            .ReturnsAsync((TransportCredential?)null);

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.None.Should().BeTrue();

        setup.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialIsMalformed_ShouldFail()
    {
        var setup = CreateTestContext();

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(new TransportCredential
            {
                Kind = TransportCredentialKind.Session,
                Value = "invalid",
                Device = CreateDeviceInfo()
            });

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should().Be("Invalid session");

        setup.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionIsInvalid_ShouldReturnNoResult()
    {
        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInvalidValidationResult());

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.None.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserKeyIsMissing_ShouldReturnNoResult()
    {
        var setup = CreateTestContext();

        var validationResult = SessionValidationResult.Active(
            tenant: Tenant,
            userKey: null,
            sessionId: SessionId,
            chainId: ChainId,
            rootId: RootId,
            claims: ClaimsSnapshot.Empty,
            authenticatedAt: Now);

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.None.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionIsValid_ShouldAuthenticateUser()
    {
        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidValidationResult());

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Ticket.Should().NotBeNull();

        result.Ticket!.AuthenticationScheme.Should()
            .Be(UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();

        result.Principal.Identity.AuthenticationType.Should()
            .Be(UAuthConstants.SchemeDefaults.GlobalScheme);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionIsValid_ShouldAddNameIdentifierClaim()
    {
        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidValidationResult());

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Principal!
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!
            .Value
            .Should()
            .Be(User.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionContainsClaims_ShouldCopyClaimsToPrincipal()
    {
        var setup = CreateTestContext();

        var validationResult = CreateValidValidationResult(
            claims: CreateClaims(
                ("uauth:permission", "users.read"),
                ("uauth:permission", "users.write"),
                ("uauth:permission", "sessions.revoke"),
                ("uauth:role", "admin")));

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validationResult);

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Succeeded.Should().BeTrue();

        result.Principal!
            .FindAll("uauth:permission")
            .Select(x => x.Value)
            .Should()
            .BeEquivalentTo(
                "users.read",
                "users.write",
                "sessions.revoke");

        result.Principal
            .FindAll("uauth:role")
            .Select(x => x.Value)
            .Should()
            .ContainSingle()
            .Which
            .Should()
            .Be("admin");
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldPassResolvedTenantToSessionValidator()
    {
        SessionValidationContext? captured = null;

        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateValidValidationResult());

        await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(Tenant);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldUseClockForValidationTime()
    {
        SessionValidationContext? captured = null;

        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateValidValidationResult());

        await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        captured.Should().NotBeNull();
        captured!.Now.Should().Be(Now);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldCreateDeviceFromTransportCredential()
    {
        var setup = CreateTestContext();

        var deviceInfo = CreateDeviceInfo();

        var transportCredential = new TransportCredential
        {
            Kind = TransportCredentialKind.Session,
            Value = SessionId,
            Device = deviceInfo
        };

        var expectedDevice = DeviceContext.Create(
            deviceInfo.DeviceId,
            deviceType: "desktop",
            platform: "desktop",
            operatingSystem: "windows 11",
            browser: "test-browser",
            ipAddress: "127.0.0.1");

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(transportCredential);

        setup.DeviceFactory
            .Setup(x => x.Create(deviceInfo))
            .Returns(expectedDevice);

        SessionValidationContext? captured = null;

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateValidValidationResult());

        var result = await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        result.Succeeded.Should().BeTrue();

        setup.DeviceFactory.Verify(
            x => x.Create(deviceInfo),
            Times.Once);

        captured.Should().NotBeNull();

        captured!.Device.Should().BeSameAs(expectedDevice);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldPassSessionIdFromTransportCredential()
    {
        SessionValidationContext? captured = null;

        var setup = CreateTestContext();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateValidValidationResult());

        await setup.HttpContext.AuthenticateAsync(
            UAuthConstants.SchemeDefaults.GlobalScheme);

        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be(SessionId);
    }

    private static TestSetup CreateTestContext(
        Mock<ITransportCredentialResolver>? credentialResolver = null,
        Mock<IDeviceContextFactory>? deviceFactory = null)
    {
        credentialResolver ??=
            new Mock<ITransportCredentialResolver>();

        deviceFactory ??=
            new Mock<IDeviceContextFactory>();

        var sessionValidator =
            new Mock<ISessionValidator>();

        var clock =
            new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        credentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>()))
            .ReturnsAsync(CreateCredential(TestSessionId));

        deviceFactory
            .Setup(x => x.Create(
                It.IsAny<DeviceInfo>()))
            .Returns(CreateDeviceContext());

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSingleton(
            credentialResolver.Object);

        services.AddSingleton(
            sessionValidator.Object);

        services.AddSingleton(
            deviceFactory.Object);

        services.AddSingleton(
            clock.Object);

        services
            .AddAuthentication(
                UAuthConstants.SchemeDefaults.GlobalScheme)
            .AddUAuthResourceApi();

        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = provider
        };

        http.Items[
            UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(Tenant);

        return new TestSetup(
            http,
            credentialResolver,
            sessionValidator,
            deviceFactory,
            clock);
    }

    private static TransportCredential CreateCredential(string value, DeviceInfo? device = null)
    {
        return new TransportCredential
        {
            Kind = TransportCredentialKind.Session,
            Value = value,
            Device = device ?? CreateDeviceInfo()
        };
    }

    private static DeviceInfo CreateDeviceInfo()
    {
        return new DeviceInfo
        {
            DeviceId = DeviceId,
            DeviceType = "desktop",
            OperatingSystem = "Windows 11",
            Platform = "desktop",
            Browser = "test-browser",
            IpAddress = "127.0.0.1"
        };
    }

    private static DeviceContext CreateDeviceContext()
    {
        return DeviceContext.Anonymous();
    }

    private static SessionValidationResult CreateInvalidValidationResult()
    {
        return SessionValidationResult.Invalid(
            SessionState.Revoked);
    }

    private static SessionValidationResult CreateValidValidationResult(
        ClaimsSnapshot? claims = null,
        UserKey? userKey = null)
    {
        return SessionValidationResult.Active(
            tenant: Tenant,
            userKey: userKey ?? User,
            sessionId: SessionId,
            chainId: ChainId,
            rootId: RootId,
            claims: claims ?? ClaimsSnapshot.Empty,
            authenticatedAt: Now);
    }

    private static ClaimsSnapshot CreateClaims(params (string Type, string Value)[] claims)
    {
        return ClaimsSnapshot.From(claims);
    }

    private const string TestSessionId = "test-session-id-00000000000000000000000000000001";

    private static AuthSessionId SessionId => AuthSessionId.Parse(TestSessionId, null);

    private static SessionChainId ChainId => SessionChainId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static SessionRootId RootId => SessionRootId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static DeviceId DeviceId => DeviceId.Create("test-device-123456789123456789123456789123456789");

    private sealed record TestSetup(
        DefaultHttpContext HttpContext,
        Mock<ITransportCredentialResolver> CredentialResolver,
        Mock<ISessionValidator> SessionValidator,
        Mock<IDeviceContextFactory> DeviceFactory,
        Mock<IClock> Clock);
}
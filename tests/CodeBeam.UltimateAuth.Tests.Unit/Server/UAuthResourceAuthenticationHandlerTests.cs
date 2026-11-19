using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthResourceAuthenticationHandlerTests
{
    private const string Scheme = UAuthConstants.SchemeDefaults.GlobalScheme;

    private const string TestSessionId =
        "test-session-id-00000000000000000000000000000001";

    private const string TestJwt = "test-jwt-access-token";

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-1");

    private static readonly UserKey User =
        UserKey.FromString("user-1");

    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AuthSessionId SessionId =>
        AuthSessionId.Parse(TestSessionId, null);

    private static SessionChainId ChainId =>
        SessionChainId.From(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static SessionRootId RootId =>
        SessionRootId.From(
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static DeviceId DeviceId =>
        DeviceId.Create(
            "test-device-123456789123456789123456789123456789");

    // ------------------------------------------------------------
    // PureOpaque / session authentication
    // ------------------------------------------------------------

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialDoesNotExist_ShouldReturnNoResult()
    {
        var setup = CreateTestContext();

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureOpaque,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync((TransportCredential?)null);

        var result = await AuthenticateAsync(setup);

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
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureOpaque,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateSessionCredential("invalid"));

        var result = await AuthenticateAsync(setup);

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
    public async Task AuthenticateAsync_WhenCredentialIsNotSession_ShouldFail()
    {
        var setup = CreateTestContext();

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureOpaque,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateJwtCredential());

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should()
            .Be("Resource authentication requires a session credential.");

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
            .ReturnsAsync(SessionValidationResult.Invalid(SessionState.Revoked));

        var result = await AuthenticateAsync(setup);

        result.None.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenUserKeyIsMissing_ShouldReturnNoResult()
    {
        var setup = CreateTestContext();

        var validation = SessionValidationResult.Active(
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
            .ReturnsAsync(validation);

        var result = await AuthenticateAsync(setup);

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

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Ticket.Should().NotBeNull();

        result.Ticket!.AuthenticationScheme.Should().Be(Scheme);
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.Identity.AuthenticationType.Should().Be(Scheme);

        setup.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
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

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        result.Principal!
            .FindFirst(ClaimTypes.NameIdentifier)!
            .Value.Should().Be(User.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionContainsClaims_ShouldCopyClaimsToPrincipal()
    {
        var setup = CreateTestContext();

        var validation = CreateValidValidationResult(
            claims: ClaimsSnapshot.From(
            [
                ("uauth:permission", "users.read"),
                ("uauth:permission", "users.write"),
                ("uauth:permission", "sessions.revoke"),
                ("uauth:role", "admin")
            ]));

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        result.Principal!
            .FindAll("uauth:permission")
            .Select(x => x.Value)
            .Should().BeEquivalentTo(
                "users.read",
                "users.write",
                "sessions.revoke");

        result.Principal
            .FindAll("uauth:role")
            .Select(x => x.Value)
            .Should().ContainSingle()
            .Which.Should().Be("admin");
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

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
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

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Now.Should().Be(Now);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldCreateDeviceFromTransportCredential()
    {
        var setup = CreateTestContext();

        var deviceInfo = CreateDeviceInfo();

        var expectedDevice = DeviceContext.Create(
            deviceInfo.DeviceId,
            deviceType: "desktop",
            platform: "desktop",
            operatingSystem: "windows 11",
            browser: "test-browser",
            ipAddress: "127.0.0.1");

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureOpaque,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateSessionCredential(TestSessionId, deviceInfo));

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

        var result = await AuthenticateAsync(setup);

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

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be(SessionId);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionIsValid_ShouldStoreValidationResult()
    {
        var setup = CreateTestContext();
        var validation = CreateValidValidationResult();

        setup.SessionValidator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        setup.HttpContext.Items[
            UAuthConstants.HttpItems.SessionValidationResult
        ].Should().BeSameAs(validation);
    }

    // ------------------------------------------------------------
    // PureJwt authentication
    // ------------------------------------------------------------

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenCredentialIsMissing_ShouldReturnNoResult()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureJwt,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync((TransportCredential?)null);

        var result = await AuthenticateAsync(setup);

        result.None.Should().BeTrue();

        setup.JwtValidator.Verify(
            x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenCredentialIsNotAccessToken_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.CredentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureJwt,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateSessionCredential(TestSessionId));

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("PureJwt requires an access token.");

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenTokenIsInvalid_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                TestJwt,
                Tenant,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidatedJwtIdentity?)null);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("Invalid JWT access token.");

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenTokenIsValid_ShouldAuthenticateUser()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                TestJwt,
                Tenant,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidatedJwt());

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Ticket.Should().NotBeNull();

        result.Ticket!.AuthenticationScheme.Should().Be(Scheme);
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();

        result.Principal.FindFirst(ClaimTypes.NameIdentifier)!
            .Value.Should().Be(User.Value);

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_ShouldPassTenantToJwtValidator()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                TestJwt,
                Tenant,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidatedJwt());

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        setup.JwtValidator.Verify(
            x => x.ValidateAsync(
                TestJwt,
                Tenant,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_ShouldUseValidatedSubjectInsteadOfUntrustedNameIdentifier()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        var validated = CreateValidatedJwt(
        [
            new Claim("sub", User.Value),
            new Claim("tenant", Tenant.Value),
            new Claim("device_id", DeviceId.Value),
            new Claim(ClaimTypes.NameIdentifier, "attacker")
        ]);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        result.Principal!
            .FindAll(ClaimTypes.NameIdentifier)
            .Select(x => x.Value)
            .Should().ContainSingle()
            .Which.Should().Be(User.Value);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_ShouldPreserveRoleClaims()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        var validated = CreateValidatedJwt(
        [
            new Claim("sub", User.Value),
            new Claim("tenant", Tenant.Value),
            new Claim("device_id", DeviceId.Value),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("uauth:permission", "products.read")
        ]);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();
        result.Principal!.IsInRole("Admin").Should().BeTrue();

        result.Principal.FindFirst("uauth:permission")!
            .Value.Should().Be("products.read");
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenSessionClaimExists_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        var validated = CreateValidatedJwt(
        [
            new Claim("sub", User.Value),
            new Claim("tenant", Tenant.Value),
            new Claim("device_id", DeviceId.Value),
            new Claim("sid", TestSessionId)
        ]);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("Session-bound JWT is not valid in PureJwt mode.");

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenDeviceClaimIsMissing_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        var validated = CreateValidatedJwt(
        [
            new Claim("sub", User.Value),
            new Claim("tenant", Tenant.Value)
        ]);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("PureJwt requires a valid device binding.");
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenDeviceClaimIsDuplicated_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        var validated = CreateValidatedJwt(
        [
            new Claim("sub", User.Value),
            new Claim("tenant", Tenant.Value),
            new Claim("device_id", DeviceId.Value),
            new Claim("device_id", DeviceId.Value)
        ]);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated);

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("PureJwt requires a valid device binding.");
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_WhenDeviceDoesNotMatch_ShouldFail()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.DeviceResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(CreateDeviceInfo(
                DeviceId.Create("another-device-123456789123456789123456789123456")));

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidatedJwt());

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeFalse();
        result.Failure!.Message.Should()
            .Be("JWT device mismatch.");

        VerifyNoSessionValidation(setup);
    }

    [Fact]
    public async Task AuthenticateAsync_PureJwt_ShouldNotStoreSessionValidationResult()
    {
        var setup = CreateTestContext(UAuthMode.PureJwt);

        setup.JwtValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<string>(),
                It.IsAny<TenantKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidatedJwt());

        var result = await AuthenticateAsync(setup);

        result.Succeeded.Should().BeTrue();

        setup.HttpContext.Items.ContainsKey(
            UAuthConstants.HttpItems.SessionValidationResult)
            .Should().BeFalse();

        VerifyNoSessionValidation(setup);
    }

    // ------------------------------------------------------------
    // Test infrastructure
    // ------------------------------------------------------------

    private static TestSetup CreateTestContext(
        UAuthMode mode = UAuthMode.PureOpaque)
    {
        var credentialResolver = new Mock<ITransportCredentialResolver>();
        var sessionValidator = new Mock<ISessionValidator>();
        var deviceFactory = new Mock<IDeviceContextFactory>();
        var deviceResolver = new Mock<IDeviceResolver>();
        var jwtValidator = new Mock<IUAuthJwtValidator>();
        var clock = new Mock<IClock>();

        clock.SetupGet(x => x.UtcNow).Returns(Now);

        deviceFactory
            .Setup(x => x.Create(It.IsAny<DeviceInfo>()))
            .Returns(DeviceContext.Anonymous());

        deviceResolver
            .Setup(x => x.ResolveAsync(It.IsAny<HttpContext>()))
            .ReturnsAsync(CreateDeviceInfo());

        credentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureOpaque,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateSessionCredential(TestSessionId));

        credentialResolver
            .Setup(x => x.ResolveAsync(
                It.IsAny<HttpContext>(),
                UAuthMode.PureJwt,
                It.IsAny<EffectiveAuthResponse>()))
            .ReturnsAsync(CreateJwtCredential());

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddSingleton<ITransportCredentialResolver>(
            credentialResolver.Object);

        services.AddSingleton<ISessionValidator>(
            sessionValidator.Object);

        services.AddSingleton<IDeviceContextFactory>(
            deviceFactory.Object);

        services.AddSingleton<IDeviceResolver>(
            deviceResolver.Object);

        services.AddSingleton<IUAuthJwtValidator>(
            jwtValidator.Object);

        services.AddSingleton<IClock>(
            clock.Object);

        services.AddSingleton<IOptions<UAuthResourceApiOptions>>(
            Options.Create(new UAuthResourceApiOptions
            {
                Auth = new UAuthResourceAuthOptions
                {
                    Mode = mode,
                    Session = new UAuthResourceSessionOptions
                    {
                        DeliveryMode = GrantDeliveryMode.Cookie
                    },
                    AccessToken = new UAuthResourceAccessTokenOptions
                    {
                        DeliveryMode = GrantDeliveryMode.Cookie
                    }
                },
            }));

        services
            .AddAuthentication(Scheme)
            .AddUAuthResourceApi();

        var provider = services.BuildServiceProvider();

        var http = new DefaultHttpContext
        {
            RequestServices = provider
        };

        http.Items[UAuthConstants.HttpItems.TenantContextKey] =
            UAuthTenantContext.Resolved(Tenant);

        return new TestSetup(
            http,
            credentialResolver,
            sessionValidator,
            deviceFactory,
            deviceResolver,
            jwtValidator,
            clock);
    }

    private static Task<AuthenticateResult> AuthenticateAsync(TestSetup setup)
    {
        return setup.HttpContext.AuthenticateAsync(Scheme);
    }

    private static TransportCredential CreateSessionCredential(
        string value,
        DeviceInfo? device = null)
    {
        return new TransportCredential
        {
            Kind = TransportCredentialKind.Session,
            Value = value,
            Device = device ?? CreateDeviceInfo()
        };
    }

    private static TransportCredential CreateJwtCredential()
    {
        return new TransportCredential
        {
            Kind = TransportCredentialKind.AccessToken,
            Value = TestJwt,
            Device = CreateDeviceInfo()
        };
    }

    private static DeviceInfo CreateDeviceInfo(DeviceId? deviceId = null)
    {
        return new DeviceInfo
        {
            DeviceId = deviceId ?? DeviceId,
            DeviceType = "desktop",
            OperatingSystem = "Windows 11",
            Platform = "desktop",
            Browser = "test-browser",
            IpAddress = "127.0.0.1"
        };
    }

    private static SessionValidationResult CreateValidValidationResult(
        ClaimsSnapshot? claims = null)
    {
        return SessionValidationResult.Active(
            tenant: Tenant,
            userKey: User,
            sessionId: SessionId,
            chainId: ChainId,
            rootId: RootId,
            claims: claims ?? ClaimsSnapshot.Empty,
            authenticatedAt: Now);
    }

    private static ValidatedJwtIdentity CreateValidatedJwt(
        IReadOnlyCollection<Claim>? claims = null)
    {
        return new ValidatedJwtIdentity
        {
            Subject = User.Value,
            TenantId = Tenant.Value,
            Issuer = "UAuth",
            Audience = "UAuthClient",
            ExpiresAt = Now.AddMinutes(30),
            Claims = claims ??
            [
                new Claim("sub", User.Value),
                new Claim("tenant", Tenant.Value),
                new Claim("device_id", DeviceId.Value)
            ]
        };
    }

    private static void VerifyNoSessionValidation(TestSetup setup)
    {
        setup.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private sealed record TestSetup(
        DefaultHttpContext HttpContext,
        Mock<ITransportCredentialResolver> CredentialResolver,
        Mock<ISessionValidator> SessionValidator,
        Mock<IDeviceContextFactory> DeviceFactory,
        Mock<IDeviceResolver> DeviceResolver,
        Mock<IUAuthJwtValidator> JwtValidator,
        Mock<IClock> Clock);
}

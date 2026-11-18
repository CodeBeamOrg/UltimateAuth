using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

public sealed class ResourceApiAuthenticationTests
{
    private const string SessionValue = "test-resource-session-000000000000000000000001";

    private static readonly TenantKey Tenant = TenantKeys.Single;

    private static readonly UserKey User = UserKey.FromString("user-1");

    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AuthSessionId SessionId => AuthSessionId.Parse(SessionValue, null);

    private static SessionChainId ChainId => SessionChainId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static SessionRootId RootId => SessionRootId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public async Task AnonymousEndpoint_WithoutCredential_ShouldReturnOk()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/anonymous",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        VerifyNeverValidated(validator);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutCredential_ShouldReturnUnauthorized()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        VerifyNeverValidated(validator);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithInvalidSession_ShouldReturnUnauthorized()
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                SessionValidationResult.Invalid(
                    SessionState.Revoked));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        VerifyValidatedAtLeastOnce(validator);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidSession_ShouldReturnOk()
    {
        var validator = CreateValidator(ClaimsSnapshot.Empty);

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        VerifyValidatedAtLeastOnce(validator);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithValidSession_ShouldPassSessionIdToValidator()
    {
        SessionValidationContext? captured = null;

        var validator = CreateValidator(ClaimsSnapshot.Empty);

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateValidationResult(ClaimsSnapshot.Empty));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        captured.Should().NotBeNull();
        captured!.SessionId.Should().Be(SessionId);
        captured.Tenant.Should().Be(Tenant);
    }

    [Fact]
    public async Task AdminEndpoint_WithoutCredential_ShouldReturnUnauthorized()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/admin",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        VerifyNeverValidated(validator);
    }

    [Fact]
    public async Task AdminEndpoint_WithAuthenticatedNonAdminUser_ShouldReturnForbidden()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "User")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/admin",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);

        VerifyValidatedAtLeastOnce(validator);
    }

    [Fact]
    public async Task AdminEndpoint_WithNoRoles_ShouldReturnForbidden()
    {
        var validator = CreateValidator(ClaimsSnapshot.Empty);

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/admin",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ShouldReturnOk()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "Admin")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/admin",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        VerifyValidatedAtLeastOnce(validator);
    }

    [Fact]
    public async Task AuthenticatedEndpoint_ShouldExposeUltimateAuthIdentityToController()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "Admin"),
                ("uauth:permission", "products.read.self"),
                ("uauth:permission", "products.update.admin")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/identity",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content
            .ReadFromJsonAsync<IdentityResponse>(
                cancellationToken: TestContext.Current.CancellationToken);

        body.Should().NotBeNull();

        body!.UserId.Should().Be(User.Value);

        body.Roles.Should()
            .ContainSingle()
            .Which.Should().Be("Admin");

        body.Permissions.Should()
            .BeEquivalentTo(
                "products.read.self",
                "products.update.admin");
    }

    [Fact]
    public async Task AuthenticatedEndpoint_WithInvalidSession_ShouldNotExposeIdentity()
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                SessionValidationResult.Invalid(
                    SessionState.Revoked));

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/identity",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(UserStatus.Active, HttpStatusCode.OK)]
    [InlineData(UserStatus.Suspended, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.SelfSuspended, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Disabled, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Unknown, HttpStatusCode.Forbidden)]
    public async Task ProtectedEndpoint_ShouldEnforceUserStatus(
        UserStatus status,
        HttpStatusCode expected)
    {
        var validator = CreateValidator(
            ClaimsSnapshot.Empty,
            status);

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);

        VerifyValidatedAtLeastOnce(validator);
    }

    [Theory]
    [InlineData(UserStatus.Active, HttpStatusCode.OK)]
    [InlineData(UserStatus.Suspended, HttpStatusCode.OK)]
    [InlineData(UserStatus.SelfSuspended, HttpStatusCode.OK)]
    [InlineData(UserStatus.Disabled, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Unknown, HttpStatusCode.Forbidden)]
    public async Task SupportTicket_ShouldAllowOnlyEligibleUsers(
        UserStatus status,
        HttpStatusCode expected)
    {
        var validator = CreateValidator(
            ClaimsSnapshot.Empty,
            status);

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        AddSession(host.Client);

        using var response = await host.Client.PostAsync(
            "/__tests/resource/support/tickets",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);

        VerifyValidatedAtLeastOnce(validator);
    }

    private static Mock<ISessionValidator> CreateValidator(
        ClaimsSnapshot claims,
        UserStatus status = UserStatus.Active)
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateValidationResult(claims, status));

        return validator;
    }

    private static SessionValidationResult CreateValidationResult(
        ClaimsSnapshot claims,
        UserStatus status = UserStatus.Active)
    {
        return SessionValidationResult.Active(
            tenant: Tenant,
            userKey: User,
            sessionId: SessionId,
            chainId: ChainId,
            rootId: RootId,
            claims: claims,
            authenticatedAt: Now,
            userStatus: status);
    }

    private static void AddSession(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                SessionValue);
    }

    private static void VerifyNeverValidated(
        Mock<ISessionValidator> validator)
    {
        validator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static void VerifyValidatedAtLeastOnce(
        Mock<ISessionValidator> validator)
    {
        validator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    private sealed class IdentityResponse
    {
        public string? UserId { get; init; }

        public string[] Roles { get; init; } = [];

        public string[] Permissions { get; init; } = [];
    }
}

using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Timers;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

public sealed class ResourceApiAuthenticationTests
{
    private const string SessionValue = "test-resource-session-000000000000000000000001";
    private static readonly TenantKey Tenant = TenantKey.FromExternal("tenant-1");
    private static readonly UserKey User = UserKey.FromString("user-1");
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static AuthSessionId SessionId => AuthSessionId.Parse(SessionValue, null);
    private static SessionChainId ChainId => SessionChainId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static SessionRootId RootId => SessionRootId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    // --------------------------------------------------
    // Anonymous
    // --------------------------------------------------

    [Fact]
    public async Task AnonymousEndpoint_WithoutCredential_ShouldReturnOk()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        var response = await host.Client.GetAsync(
            "/__tests/resource/anonymous");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        validator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --------------------------------------------------
    // 401
    // --------------------------------------------------

    [Fact]
    public async Task ProtectedEndpoint_WithoutCredential_ShouldReturnUnauthorized()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        validator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --------------------------------------------------
    // Invalid session -> 401
    // --------------------------------------------------

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
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);

        validator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    // --------------------------------------------------
    // Valid session -> 200
    // --------------------------------------------------

    [Fact]
    public async Task ProtectedEndpoint_WithValidSession_ShouldReturnOk()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.Empty);

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    // --------------------------------------------------
    // Valid session but missing role -> 403
    // --------------------------------------------------

    [Fact]
    public async Task AdminEndpoint_WithAuthenticatedNonAdminUser_ShouldReturnForbidden()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "User")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/admin");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);
    }

    // --------------------------------------------------
    // Valid Admin -> 200
    // --------------------------------------------------

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_ShouldReturnOk()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "Admin")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/admin");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    // --------------------------------------------------
    // Claims really reach developer's controller
    // --------------------------------------------------

    [Fact]
    public async Task AuthenticatedEndpoint_ShouldExposeUltimateAuthIdentityToController()
    {
        var validator = CreateValidator(
            ClaimsSnapshot.From(
                (ClaimTypes.Role, "Admin"),
                ("uauth:permission", "products.read.self"),
                ("uauth:permission", "products.update.admin")));

        await using var host =
            await ResourceApiTestHost.CreateAsync(
                validator.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/identity");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var body =
            await response.Content
                .ReadFromJsonAsync<IdentityResponse>();

        body.Should().NotBeNull();

        body!.UserId.Should().Be(User.Value);

        body.Roles.Should()
            .ContainSingle()
            .Which.Should()
            .Be("Admin");

        body.Permissions.Should()
            .BeEquivalentTo(
                "products.read.self",
                "products.update.admin");
    }

    private static Mock<ISessionValidator> CreateValidator(
        ClaimsSnapshot claims)
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                SessionValidationResult.Active(
                    tenant: Tenant,
                    userKey: User,
                    sessionId: SessionId,
                    chainId: ChainId,
                    rootId: RootId,
                    claims: claims,
                    authenticatedAt: Now));

        return validator;
    }

    private static void AddSession(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                SessionValue);
    }

    private sealed class IdentityResponse
    {
        public string? UserId { get; init; }

        public string[] Roles { get; init; } = [];

        public string[] Permissions { get; init; } = [];
    }
}
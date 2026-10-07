using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using FluentAssertions;
using Moq;
using System.Net;
using System.Net.Http.Headers;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

public sealed class ResourceApiTenantIsolationTests
{
    private const string SessionValue =
        "test-resource-session-000000000000000000000001";

    private const string TenantHeader = "X-Tenant-Id";

    private static readonly TenantKey TenantA =
        TenantKey.FromExternal("tenant-a");

    private static readonly TenantKey TenantB =
        TenantKey.FromExternal("tenant-b");

    private static readonly UserKey User =
        UserKey.FromString("user-1");

    private static readonly DateTimeOffset Now =
        new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static AuthSessionId SessionId =>
        AuthSessionId.Parse(SessionValue, null);

    private static SessionChainId ChainId =>
        SessionChainId.From(
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static SessionRootId RootId =>
        SessionRootId.From(
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

    [Fact]
    public async Task RequestTenant_ShouldBePassedToSessionValidation()
    {
        SessionValidationContext? captured = null;

        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) => captured = context)
            .ReturnsAsync(CreateActiveResult(TenantA));

        await using var host =
            await CreateMultiTenantHostAsync(validator.Object);

        AddSession(host.Client);
        AddTenant(host.Client, "tenant-a");

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        captured.Should().NotBeNull();

        captured!.Tenant.Should()
            .Be(TenantA);
    }

    [Fact]
    public async Task TenantA_WithTenantASession_ShouldAuthenticate()
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(
                    context => context.Tenant == TenantA),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveResult(TenantA));

        await using var host =
            await CreateMultiTenantHostAsync(validator.Object);

        AddSession(host.Client);
        AddTenant(host.Client, "tenant-a");

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TenantB_WithTenantASession_ShouldNotAuthenticate()
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(
                    context => context.Tenant == TenantB),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                SessionValidationResult.Invalid(
                    SessionState.NotFound));

        await using var host =
            await CreateMultiTenantHostAsync(validator.Object);

        AddSession(host.Client);
        AddTenant(host.Client, "tenant-b");

        var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SameSessionId_ShouldRemainTenantScoped()
    {
        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(
                    context =>
                        context.Tenant == TenantA &&
                        context.SessionId == SessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateActiveResult(TenantA));

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.Is<SessionValidationContext>(
                    context =>
                        context.Tenant == TenantB &&
                        context.SessionId == SessionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                SessionValidationResult.Invalid(
                    SessionState.NotFound));

        await using var host =
            await CreateMultiTenantHostAsync(validator.Object);

        AddSession(host.Client);
        AddTenant(host.Client, "tenant-a");

        var tenantAResponse = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        tenantAResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        host.Client.DefaultRequestHeaders.Remove(TenantHeader);
        AddTenant(host.Client, "tenant-b");

        var tenantBResponse = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        tenantBResponse.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResolvedTenant_ShouldBePreservedInValidationContext()
    {
        var observedTenants = new List<TenantKey>();

        var validator = new Mock<ISessionValidator>();

        validator
            .Setup(x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<SessionValidationContext, CancellationToken>(
                (context, _) =>
                {
                    observedTenants.Add(context.Tenant);
                })
            .ReturnsAsync(
                (SessionValidationContext context, CancellationToken _) =>
                    CreateActiveResult(context.Tenant));

        await using var host =
            await CreateMultiTenantHostAsync(validator.Object);

        AddSession(host.Client);

        AddTenant(host.Client, "tenant-a");

        var tenantAResponse = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        host.Client.DefaultRequestHeaders.Remove(TenantHeader);

        AddTenant(host.Client, "tenant-b");

        var tenantBResponse = await host.Client.GetAsync(
            "/__tests/resource/authenticated");

        tenantAResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        tenantBResponse.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        observedTenants.Should()
            .Contain(TenantA);

        observedTenants.Should()
            .Contain(TenantB);
    }

    private static SessionValidationResult CreateActiveResult(
        TenantKey tenant)
    {
        return SessionValidationResult.Active(
            tenant: tenant,
            userKey: User,
            sessionId: SessionId,
            chainId: ChainId,
            rootId: RootId,
            claims: ClaimsSnapshot.Empty,
            authenticatedAt: Now);
    }

    private static void AddSession(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                SessionValue);
    }

    private static void AddTenant(
        HttpClient client,
        string tenant)
    {
        client.DefaultRequestHeaders.Add(
            TenantHeader,
            tenant);
    }

    private static async Task<ResourceApiTestHost> CreateMultiTenantHostAsync(ISessionValidator validator)
    {
        return await ResourceApiTestHost.CreateAsync(validator,
            configureResourceApi: options =>
            {
                options.MultiTenant.Enabled = true;
                options.MultiTenant.EnableHeader = true;
                options.MultiTenant.HeaderName = TenantHeader;
            });
    }
}

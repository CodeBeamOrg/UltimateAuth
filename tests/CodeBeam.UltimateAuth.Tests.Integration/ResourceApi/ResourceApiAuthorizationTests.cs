using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Server;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net;
using System.Net.Http.Headers;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

public sealed class ResourceApiAuthorizationTests
{
    private const string SessionValue =
        "test-resource-session-000000000000000000000001";

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-1");

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
    public async Task PolicyEndpoint_WithoutCredential_ShouldReturnUnauthorized()
    {
        var validator = new Mock<ISessionValidator>();

        await using var host =
            await ResourceApiTestHost.CreateAsync(validator.Object);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PolicyEndpoint_WithInvalidSession_ShouldReturnUnauthorized()
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

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PolicyEndpoint_WhenAuthorityAllows_ShouldReturnOk()
    {
        var validator = CreateValidValidator(
            ("uauth:permission", TestResourceController.ProductsRead));

        var authority = CreateAllowAuthority();
        var policyProvider = CreatePolicyProvider();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        authority.Verify(
            x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()),
            Times.Once);
    }

    [Fact]
    public async Task PolicyEndpoint_WhenAuthorityDenies_ShouldReturnForbidden()
    {
        var validator = CreateValidValidator();

        var authority = CreateDenyAuthority();
        var policyProvider = CreatePolicyProvider();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ProductsReadPolicy_ShouldBuildExpectedAccessContext()
    {
        var validator = CreateValidValidator(
            ("uauth:permission", TestResourceController.ProductsRead));

        AccessContext? captured = null;

        var authority = new Mock<IAccessAuthority>();

        authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Callback<AccessContext, IEnumerable<IAccessPolicy>>(
                (context, _) => captured = context)
            .Returns(CreateAllowedDecision());

        var policyProvider = CreatePolicyProvider();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        captured.Should().NotBeNull();

        captured!.Action.Should()
            .Be(TestResourceController.ProductsRead);

        captured.Resource.Should()
            .Be("products");

        captured.ActorUserKey.Should()
            .Be(User);

        captured.ActorTenant.Should()
            .Be(TenantKey.Single);

        captured.ResourceTenant.Should()
            .Be(TenantKey.Single);

        captured.IsAuthenticated.Should()
            .BeTrue();
    }

    [Fact]
    public async Task PolicyAuthorization_ShouldEnrichAccessContextWithPermissionsFromClaims()
    {
        var validator = CreateValidValidator(
            ("uauth:permission", TestResourceController.ProductsRead),
            ("uauth:permission", "orders.read.self"));

        AccessContext? captured = null;

        var authority = new Mock<IAccessAuthority>();

        authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Callback<AccessContext, IEnumerable<IAccessPolicy>>(
                (context, _) => captured = context)
            .Returns(CreateAllowedDecision());

        var policyProvider = CreatePolicyProvider();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        captured.Should().NotBeNull();

        captured!.Attributes.Should()
            .ContainKey(UAuthConstants.Access.Permissions);

        captured.Attributes[UAuthConstants.Access.Permissions]
            .Should()
            .BeOfType<CompiledPermissionSet>();
    }

    [Fact]
    public async Task PolicyAuthorization_ShouldPassEnrichedContextToPolicyProvider()
    {
        var validator = CreateValidValidator(
            ("uauth:permission", TestResourceController.ProductsRead));

        AccessContext? policyContext = null;

        var policyProvider = new Mock<IAccessPolicyProvider>();

        policyProvider
            .Setup(x => x.GetPolicies(
                It.IsAny<AccessContext>()))
            .Callback<AccessContext>(
                context => policyContext = context)
            .Returns(Array.Empty<IAccessPolicy>());

        var authority = CreateAllowAuthority();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        var response = await host.Client.GetAsync(
            "/__tests/resource/products/read");

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        policyContext.Should().NotBeNull();

        policyContext!.Attributes.Should()
            .ContainKey(UAuthConstants.Access.Permissions);

        policyContext.Attributes[UAuthConstants.Access.Permissions]
            .Should()
            .BeOfType<CompiledPermissionSet>();
    }

    [Fact]
    public async Task ProductsUpdatePolicy_ShouldPreserveFullActionAndResolveProductsResource()
    {
        var validator = CreateValidValidator(
            ("uauth:permission", TestResourceController.ProductsUpdate));

        AccessContext? captured = null;

        var authority = new Mock<IAccessAuthority>();

        authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Callback<AccessContext, IEnumerable<IAccessPolicy>>(
                (context, _) => captured = context)
            .Returns(CreateAllowedDecision());

        var policyProvider = CreatePolicyProvider();

        await using var host =
            await CreateHostAsync(
                validator.Object,
                authority.Object,
                policyProvider.Object);

        AddSession(host.Client);

        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            "/__tests/resource/products/42");

        var response = await host.Client.SendAsync(request);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        captured.Should().NotBeNull();

        captured!.Action.Should()
            .Be(TestResourceController.ProductsUpdate);

        captured.Resource.Should()
            .Be("products");
    }

    private static async Task<ResourceApiTestHost> CreateHostAsync(
        ISessionValidator validator,
        IAccessAuthority authority,
        IAccessPolicyProvider policyProvider)
    {
        return await ResourceApiTestHost.CreateAsync(
            validator,
            services =>
            {
                services.AddScoped<IAccessAuthority>(
                    _ => authority);

                services.AddScoped<IAccessPolicyProvider>(
                    _ => policyProvider);
            });
    }

    private static Mock<IAccessPolicyProvider> CreatePolicyProvider()
    {
        var provider = new Mock<IAccessPolicyProvider>();

        provider
            .Setup(x => x.GetPolicies(
                It.IsAny<AccessContext>()))
            .Returns(Array.Empty<IAccessPolicy>());

        return provider;
    }

    private static Mock<IAccessAuthority> CreateAllowAuthority()
    {
        var authority = new Mock<IAccessAuthority>();

        authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Returns(CreateAllowedDecision());

        return authority;
    }

    private static Mock<IAccessAuthority> CreateDenyAuthority()
    {
        var authority = new Mock<IAccessAuthority>();

        authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Returns(CreateDeniedDecision());

        return authority;
    }

    private static Mock<ISessionValidator> CreateValidValidator(
        params (string Type, string Value)[] claims)
    {
        var validator = new Mock<ISessionValidator>();

        var snapshot = claims.Length == 0
            ? ClaimsSnapshot.Empty
            : ClaimsSnapshot.From(claims);

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
                    claims: snapshot,
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

    private static AccessDecision CreateAllowedDecision()
    {
        return AccessDecision.Allow();
    }

    private static AccessDecision CreateDeniedDecision()
    {
        return AccessDecision.Deny("missing_permission");
    }
}

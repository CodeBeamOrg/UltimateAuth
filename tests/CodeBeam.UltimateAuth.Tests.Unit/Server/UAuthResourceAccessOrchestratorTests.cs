using System.Security.Claims;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Server.Authorization;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthResourceAccessOrchestratorTests
{
    private readonly Mock<IAccessAuthority> _authority = new();
    private readonly Mock<IAccessPolicyProvider> _policyProvider = new();

    private readonly DefaultHttpContext _httpContext;
    private readonly UAuthResourceAccessOrchestrator _sut;

    public UAuthResourceAccessOrchestratorTests()
    {
        _httpContext = new DefaultHttpContext();

        var accessor = new HttpContextAccessor
        {
            HttpContext = _httpContext
        };

        _sut = new UAuthResourceAccessOrchestrator(
            _authority.Object,
            _policyProvider.Object,
            accessor);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAllowed_ShouldExecuteCommandExactlyOnce()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Allow());

        var executions = 0;

        var command = new AccessCommand(_ =>
        {
            executions++;
            return Task.CompletedTask;
        });

        await _sut.ExecuteAsync(context, command);

        executions.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WhenDenied_ShouldThrowAuthorizationException()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Deny("permission_required"));

        var command = new AccessCommand(_ => Task.CompletedTask);

        var act = () => _sut.ExecuteAsync(context, command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*permission_required*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenDenied_ShouldNeverExecuteCommand()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Deny("denied"));

        var executed = false;

        var command = new AccessCommand(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        var act = () => _sut.ExecuteAsync(context, command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenDeniedWithoutReason_ShouldUseDefaultReason()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Deny(null));

        var command = new AccessCommand(_ => Task.CompletedTask);

        var act = () => _sut.ExecuteAsync(context, command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*authorization_denied*");
    }

    [Fact]
    public async Task ExecuteAsync_WhenReauthenticationRequired_ShouldNotExecuteCommand()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.ReauthenticationRequired());

        var executed = false;

        var command = new AccessCommand(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        var act = () => _sut.ExecuteAsync(context, command);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*reauthentication*");

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassEnrichedContextToPolicyProvider()
    {
        _httpContext.User = CreatePrincipal(
            ("uauth:permission", "users.read"),
            ("uauth:permission", "users.write"));

        var original = CreateContext();

        AccessContext? captured = null;

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(x => captured = x)
            .Returns(Array.Empty<IAccessPolicy>());

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Returns(AccessDecision.Allow());

        await _sut.ExecuteAsync(
            original,
            new AccessCommand(_ => Task.CompletedTask));

        captured.Should().NotBeNull();

        var compiled = GetCompiledPermissions(captured!);

        compiled.IsAllowed("users.read").Should().BeTrue();
        compiled.IsAllowed("users.write").Should().BeTrue();
        compiled.IsAllowed("users.delete").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCompileWildcardPermissionClaims()
    {
        _httpContext.User = CreatePrincipal(
            ("uauth:permission", "users.*"));

        AccessContext? captured = null;

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(x => captured = x)
            .Returns(Array.Empty<IAccessPolicy>());

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Returns(AccessDecision.Allow());

        await _sut.ExecuteAsync(
            CreateContext(),
            new AccessCommand(_ => Task.CompletedTask));

        var compiled = GetCompiledPermissions(captured!);

        compiled.IsAllowed("users.read").Should().BeTrue();
        compiled.IsAllowed("users.delete").Should().BeTrue();
        compiled.IsAllowed("roles.read").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPermissionClaims_ShouldProvideEmptyPermissionSet()
    {
        _httpContext.User = CreatePrincipal(
            (ClaimTypes.Name, "test-user"));

        AccessContext? captured = null;

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(x => captured = x)
            .Returns(Array.Empty<IAccessPolicy>());

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()))
            .Returns(AccessDecision.Allow());

        await _sut.ExecuteAsync(
            CreateContext(),
            new AccessCommand(_ => Task.CompletedTask));

        var compiled = GetCompiledPermissions(captured!);

        compiled.IsAllowed("users.read").Should().BeFalse();
        compiled.IsAllowed("*").Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassEnrichedContextToAuthority()
    {
        _httpContext.User = CreatePrincipal(
            ("uauth:permission", "orders.read"));

        AccessContext? authorityContext = null;

        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Callback<AccessContext, IEnumerable<IAccessPolicy>>(
                (context, _) => authorityContext = context)
            .Returns(AccessDecision.Allow());

        await _sut.ExecuteAsync(
            CreateContext(),
            new AccessCommand(_ => Task.CompletedTask));

        authorityContext.Should().NotBeNull();

        var compiled = GetCompiledPermissions(authorityContext!);

        compiled.IsAllowed("orders.read").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsyncOfT_WhenAllowed_ShouldReturnCommandResult()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Allow());

        var command = new AccessCommand<string>(
            _ => Task.FromResult("result"));

        var result = await _sut.ExecuteAsync(context, command);

        result.Should().Be("result");
    }

    [Fact]
    public async Task ExecuteAsyncOfT_WhenDenied_ShouldNeverExecuteCommand()
    {
        var context = CreateContext();
        var policies = Array.Empty<IAccessPolicy>();

        _policyProvider
            .Setup(x => x.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(policies);

        _authority
            .Setup(x => x.Decide(
                It.IsAny<AccessContext>(),
                policies))
            .Returns(AccessDecision.Deny("forbidden"));

        var executed = false;

        var command = new AccessCommand<string>(_ =>
        {
            executed = true;
            return Task.FromResult("result");
        });

        var act = () => _sut.ExecuteAsync(context, command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancellationRequested_ShouldNotEvaluateAuthorization()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var command = new AccessCommand(_ => Task.CompletedTask);

        var act = () =>
            _sut.ExecuteAsync(
                CreateContext(),
                command,
                cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _policyProvider.Verify(
            x => x.GetPolicies(It.IsAny<AccessContext>()),
            Times.Never);

        _authority.Verify(
            x => x.Decide(
                It.IsAny<AccessContext>(),
                It.IsAny<IEnumerable<IAccessPolicy>>()),
            Times.Never);
    }

    private static ClaimsPrincipal CreatePrincipal(
        params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                claims.Select(x => new Claim(x.Type, x.Value)),
                authenticationType: "test"));
    }

    private static AccessContext CreateContext()
    {
        return new AccessContext(
            actorUserKey: null,
            actorTenant: default,
            isAuthenticated: true,
            isSystemActor: false,
            actorChainId: null,
            resource: "users",
            targetUserKey: null,
            resourceTenant: default,
            action: "users.read",
            attributes: EmptyAttributes.Instance);
    }

    private static CompiledPermissionSet GetCompiledPermissions(
        AccessContext context)
    {
        context.Attributes
            .TryGetValue(
                UAuthConstants.Access.Permissions,
                out var value)
            .Should()
            .BeTrue();

        return value.Should()
            .BeOfType<CompiledPermissionSet>()
            .Subject;
    }
}
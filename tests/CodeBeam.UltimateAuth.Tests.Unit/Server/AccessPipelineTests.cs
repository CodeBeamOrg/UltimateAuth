using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Policies.Abstractions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class AccessPipelineTests
{
    [Fact]
    public async Task Allowed_pipeline_should_execute_command_once()
    {
        var pipeline = CreatePipeline();
        var executions = 0;

        var command = new AccessCommand(_ =>
        {
            executions++;
            return Task.CompletedTask;
        });

        await pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.read"),
            command);

        executions.Should().Be(1);
    }

    [Fact]
    public async Task Generic_pipeline_should_return_command_result()
    {
        var pipeline = CreatePipeline();
        var executions = 0;

        var command = new AccessCommand<string>(_ =>
        {
            executions++;
            return Task.FromResult("expected-result");
        });

        var result = await pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.read"),
            command);

        result.Should().Be("expected-result");
        executions.Should().Be(1);
    }

    [Fact]
    public async Task Invariant_denial_should_stop_entire_policy_pipeline()
    {
        var trace = new List<string>();

        var invariant = Invariant(_ =>
        {
            trace.Add("invariant");
            return AccessDecision.Deny("invariant_denied");
        });

        var global = Policy(
            _ => true,
            _ =>
            {
                trace.Add("global");
                return AccessDecision.Allow();
            });

        var runtime = Policy(
            _ => true,
            _ =>
            {
                trace.Add("runtime");
                return AccessDecision.Allow();
            });

        var pipeline = CreatePipeline(
            invariants: new[] { invariant },
            globalPolicies: new[] { global },
            runtimePolicies: new[] { runtime });

        var executed = false;

        var command = new AccessCommand(_ =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*invariant_denied*");

        trace.Should().Equal("invariant");
        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Global_policy_denial_should_prevent_runtime_policy_and_command()
    {
        var trace = new List<string>();

        var global = Policy(
            _ => true,
            _ =>
            {
                trace.Add("global");
                return AccessDecision.Deny("global_denied");
            });

        var runtime = Policy(
            _ => true,
            _ =>
            {
                trace.Add("runtime");
                return AccessDecision.Allow();
            });

        var pipeline = CreatePipeline(
            globalPolicies: new[] { global },
            runtimePolicies: new[] { runtime });

        var executed = false;

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            new AccessCommand(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }));

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*global_denied*");

        trace.Should().Equal("global");
        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Non_applicable_policies_should_not_execute_decide()
    {
        var skipped = Policy(
            _ => false,
            _ => throw new Exception("Should not be evaluated"));

        var pipeline = CreatePipeline(
            globalPolicies: new[] { skipped },
            runtimePolicies: new[] { skipped });

        var executed = false;

        await pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.read"),
            new AccessCommand(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }));

        executed.Should().BeTrue();

        skipped.Verify(
            p => p.Decide(It.IsAny<AccessContext>()),
            Times.Never);
    }

    [Fact]
    public async Task Global_allow_should_not_skip_runtime_denial()
    {
        var global = Policy(
            _ => true,
            _ => AccessDecision.Allow());

        var runtime = Policy(
            _ => true,
            _ => AccessDecision.Deny("runtime_denied"));

        var pipeline = CreatePipeline(
            globalPolicies: new[] { global },
            runtimePolicies: new[] { runtime });

        var executed = false;

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            new AccessCommand(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }));

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*runtime_denied*");

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Later_runtime_denial_should_override_earlier_allow()
    {
        var trace = new List<string>();

        var first = Policy(
            _ => true,
            _ =>
            {
                trace.Add("allow");
                return AccessDecision.Allow();
            });

        var second = Policy(
            _ => true,
            _ =>
            {
                trace.Add("deny");
                return AccessDecision.Deny("permission_required");
            });

        var third = Policy(
            _ => true,
            _ =>
            {
                trace.Add("unexpected");
                return AccessDecision.Allow();
            });

        var pipeline = CreatePipeline(
            runtimePolicies: new[] { first, second, third });

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            new AccessCommand(_ => Task.CompletedTask));

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*permission_required*");

        trace.Should().Equal("allow", "deny");
    }

    [Fact]
    public async Task Denial_without_reason_should_use_default_reason()
    {
        var deny = Policy(
            _ => true,
            _ => AccessDecision.Deny(null!));

        var pipeline = CreatePipeline(
            runtimePolicies: new[] { deny });

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            new AccessCommand(_ => Task.CompletedTask));

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>()
            .WithMessage("*authorization_denied*");
    }

    [Fact]
    public async Task Generic_command_should_not_execute_when_denied()
    {
        var deny = Policy(
            _ => true,
            _ => AccessDecision.Deny("denied"));

        var pipeline = CreatePipeline(
            runtimePolicies: new[] { deny });

        var executions = 0;

        var command = new AccessCommand<int>(_ =>
        {
            executions++;
            return Task.FromResult(42);
        });

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.delete"),
            command);

        await act.Should()
            .ThrowAsync<UAuthAuthorizationException>();

        executions.Should().Be(0);
    }

    [Fact]
    public void Runtime_reauthentication_should_be_reported_by_authority()
    {
        var first = Policy(
            _ => true,
            _ => AccessDecision.Allow());

        var second = Policy(
            _ => true,
            _ => AccessDecision.ReauthenticationRequired());

        var authority = new UAuthAccessAuthority(
            Array.Empty<IAccessInvariant>(),
            Array.Empty<IAccessPolicy>());

        var decision = authority.Decide(
            TestAccessContext.WithAction("users.update"),
            new[] { first.Object, second.Object });

        decision.RequiresReauthentication.Should().BeTrue();
        decision.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void Runtime_denial_should_take_priority_over_earlier_reauthentication()
    {
        var reauth = Policy(
            _ => true,
            _ => AccessDecision.ReauthenticationRequired());

        var deny = Policy(
            _ => true,
            _ => AccessDecision.Deny("explicit_denial"));

        var authority = new UAuthAccessAuthority(
            Array.Empty<IAccessInvariant>(),
            Array.Empty<IAccessPolicy>());

        var decision = authority.Decide(
            TestAccessContext.WithAction("users.update"),
            new[] { reauth.Object, deny.Object });

        decision.IsDenied.Should().BeTrue();
        decision.DenyReason.Should().Be("explicit_denial");
    }

    [Fact]
    public async Task Reauthentication_should_never_execute_command()
    {
        var reauth = Policy(
            _ => true,
            _ => AccessDecision.ReauthenticationRequired());

        var pipeline = CreatePipeline(
            runtimePolicies: new[] { reauth });

        var executed = false;

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.update"),
            new AccessCommand(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }));

        await act.Should()
            .ThrowAsync<UAuthChallengeRequiredException>()
            .WithMessage("*Requires reauthentication*");

        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Authenticated_actor_should_receive_compiled_permissions()
    {
        var user = UserKey.New();
        var context = TestAccessContext.ForUser(
            user,
            "users.read");

        var pipeline = CreatePipeline(
            permissions: new[]
            {
                Permission.From("users.read"),
                Permission.From("roles.*")
            });

        AccessContext? captured = null;

        pipeline.PolicyProvider
            .Setup(p => p.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(c => captured = c)
            .Returns(Array.Empty<IAccessPolicy>());

        await pipeline.Orchestrator.ExecuteAsync(
            context,
            new AccessCommand(_ => Task.CompletedTask));

        captured.Should().NotBeNull();
        captured.Should().NotBeSameAs(context);

        captured!.Attributes.Should()
            .ContainKey(UAuthConstants.Access.Permissions);

        var compiled = captured.Attributes[
            UAuthConstants.Access.Permissions]
            .Should().BeOfType<CompiledPermissionSet>()
            .Subject;

        compiled.IsAllowed("users.read").Should().BeTrue();
        compiled.IsAllowed("roles.create").Should().BeTrue();
        compiled.IsAllowed("users.delete").Should().BeFalse();

        context.Attributes.Should()
            .NotContainKey(UAuthConstants.Access.Permissions);

        pipeline.PermissionStore.Verify(
            s => s.GetPermissionsAsync(
                context.ResourceTenant,
                user,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Authority_should_receive_same_enriched_context_as_policy_provider()
    {
        var context = TestAccessContext.ForUser(
            UserKey.New(),
            "users.read");

        AccessContext? providerContext = null;
        AccessContext? authorityContext = null;

        var runtime = Policy(
            _ => true,
            c =>
            {
                authorityContext = c;
                return AccessDecision.Allow();
            });

        var pipeline = CreatePipeline(
            permissions: new[] { Permission.From("users.read") },
            runtimePolicies: new[] { runtime });

        pipeline.PolicyProvider
            .Setup(p => p.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(c => providerContext = c)
            .Returns(new[] { runtime.Object });

        await pipeline.Orchestrator.ExecuteAsync(
            context,
            new AccessCommand(_ => Task.CompletedTask));

        providerContext.Should().NotBeNull();
        authorityContext.Should().BeSameAs(providerContext);
        providerContext.Should().NotBeSameAs(context);
    }

    [Fact]
    public async Task Anonymous_actor_should_skip_permission_store()
    {
        var context = TestAccessContext.WithAction("public.read");
        var pipeline = CreatePipeline();

        AccessContext? captured = null;

        pipeline.PolicyProvider
            .Setup(p => p.GetPolicies(It.IsAny<AccessContext>()))
            .Callback<AccessContext>(c => captured = c)
            .Returns(Array.Empty<IAccessPolicy>());

        await pipeline.Orchestrator.ExecuteAsync(
            context,
            new AccessCommand(_ => Task.CompletedTask));

        pipeline.PermissionStore.Verify(
            s => s.GetPermissionsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        captured.Should().BeSameAs(context);
    }

    [Fact]
    public async Task Permission_store_should_receive_original_cancellation_token()
    {
        using var cts = new CancellationTokenSource();

        var context = TestAccessContext.ForUser(
            UserKey.New(),
            "users.read");

        var pipeline = CreatePipeline();

        CancellationToken receivedByCommand = default;

        await pipeline.Orchestrator.ExecuteAsync(
            context,
            new AccessCommand(ct =>
            {
                receivedByCommand = ct;
                return Task.CompletedTask;
            }),
            cts.Token);

        pipeline.PermissionStore.Verify(
            s => s.GetPermissionsAsync(
                context.ResourceTenant,
                context.ActorUserKey!.Value,
                cts.Token),
            Times.Once);

        receivedByCommand.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Pre_cancelled_token_should_stop_pipeline_before_enrichment()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var pipeline = CreatePipeline();
        var executed = false;

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.ForUser(
                UserKey.New(),
                "users.read"),
            new AccessCommand(_ =>
            {
                executed = true;
                return Task.CompletedTask;
            }),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        executed.Should().BeFalse();

        pipeline.PermissionStore.VerifyNoOtherCalls();
        pipeline.PolicyProvider.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Generic_command_should_receive_cancellation_token()
    {
        using var cts = new CancellationTokenSource();

        var pipeline = CreatePipeline();
        CancellationToken received = default;

        var result = await pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.count"),
            new AccessCommand<int>(ct =>
            {
                received = ct;
                return Task.FromResult(42);
            }),
            cts.Token);

        result.Should().Be(42);
        received.Should().Be(cts.Token);
    }

    [Fact]
    public async Task Command_exception_should_propagate()
    {
        var pipeline = CreatePipeline();

        var expected = new InvalidOperationException("command_failed");

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.read"),
            new AccessCommand(_ => Task.FromException(expected)));

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        exception.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Generic_command_exception_should_propagate()
    {
        var pipeline = CreatePipeline();

        var expected = new InvalidOperationException("query_failed");

        var act = () => pipeline.Orchestrator.ExecuteAsync(
            TestAccessContext.WithAction("users.read"),
            new AccessCommand<int>(
                _ => Task.FromException<int>(expected)));

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        exception.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public void Authority_should_allow_when_no_policies_exist()
    {
        // Documents current default-allow behavior.
        var authority = new UAuthAccessAuthority(
            Array.Empty<IAccessInvariant>(),
            Array.Empty<IAccessPolicy>());

        var decision = authority.Decide(
            TestAccessContext.WithAction("users.delete"),
            Array.Empty<IAccessPolicy>());

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Authority_should_treat_null_global_collections_as_empty()
    {
        var authority = new UAuthAccessAuthority(
            null!,
            null!);

        var decision = authority.Decide(
            TestAccessContext.WithAction("users.read"),
            Array.Empty<IAccessPolicy>());

        decision.IsAllowed.Should().BeTrue();
    }

    private static PipelineSetup CreatePipeline(
        IEnumerable<Mock<IAccessInvariant>>? invariants = null,
        IEnumerable<Mock<IAccessPolicy>>? globalPolicies = null,
        IEnumerable<Mock<IAccessPolicy>>? runtimePolicies = null,
        IReadOnlyCollection<Permission>? permissions = null)
    {
        var store = new Mock<IUserPermissionStore>();
        var provider = new Mock<IAccessPolicyProvider>();

        store
            .Setup(s => s.GetPermissionsAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                permissions ?? Array.Empty<Permission>());

        var runtime = runtimePolicies?
            .Select(p => p.Object)
            .ToArray() ?? Array.Empty<IAccessPolicy>();

        provider
            .Setup(p => p.GetPolicies(It.IsAny<AccessContext>()))
            .Returns(runtime);

        var authority = new UAuthAccessAuthority(
            invariants?.Select(i => i.Object)
                ?? Array.Empty<IAccessInvariant>(),
            globalPolicies?.Select(p => p.Object)
                ?? Array.Empty<IAccessPolicy>());

        var orchestrator = new UAuthAccessOrchestrator(
            authority,
            provider.Object,
            store.Object);

        return new PipelineSetup(
            orchestrator,
            provider,
            store);
    }

    private static Mock<IAccessInvariant> Invariant(
        Func<AccessContext, AccessDecision> decide)
    {
        var mock = new Mock<IAccessInvariant>();

        mock.Setup(i => i.Decide(It.IsAny<AccessContext>()))
            .Returns((AccessContext context) => decide(context));

        return mock;
    }

    private static Mock<IAccessPolicy> Policy(
        Func<AccessContext, bool> appliesTo,
        Func<AccessContext, AccessDecision> decide)
    {
        var mock = new Mock<IAccessPolicy>();

        mock.Setup(p => p.AppliesTo(It.IsAny<AccessContext>()))
            .Returns((AccessContext context) => appliesTo(context));

        mock.Setup(p => p.Decide(It.IsAny<AccessContext>()))
            .Returns((AccessContext context) => decide(context));

        return mock;
    }

    private sealed record PipelineSetup(
        UAuthAccessOrchestrator Orchestrator,
        Mock<IAccessPolicyProvider> PolicyProvider,
        Mock<IUserPermissionStore> PermissionStore);
}

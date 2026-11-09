using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Server.Auth;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Security;

public sealed class ValidatedRollbackContextTests
{
    [Fact]
    public async Task Rollback_uses_explicit_request_flow_even_after_another_context_is_created()
    {
        var owner = LabDatabase.OwnerA;
        var target = UserKey.FromString("created-user");
        var validated = Contexts.AuthenticatedFlow(owner);
        var otherFlow = Contexts.AuthenticatedFlow(LabDatabase.OwnerB);
        var flows = new Mock<IAuthFlow>(MockBehavior.Strict);
        flows.SetupSequence(x => x.BeginAsync(AuthFlowType.ValidateSession, It.IsAny<CancellationToken>()))
            .ReturnsAsync(validated).ReturnsAsync(otherFlow);
        var factory = new Mock<IAccessContextFactory>(MockBehavior.Strict);
        factory.Setup(x => x.CreateAsync(It.IsAny<AuthFlowContext>(), It.IsAny<string>(), "users", It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .Returns((AuthFlowContext flow, string action, string resource, string? id, IDictionary<string, object>? attributes, CancellationToken ct) =>
                Task.FromResult(Contexts.Create(action, flow.UserKey, id is null ? null : UserKey.FromString(id), resource: resource)));
        var provider = new LabAccessContextProvider(flows.Object, factory.Object);
        var request = await provider.CreateValidatedAsync(UAuthActions.Users.CreateAdmin, "users", TestContext.Current.CancellationToken);
        await provider.CreateAsync(UAuthActions.Users.CreateAdmin, "users", TestContext.Current.CancellationToken);
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var rollback = await provider.CreateForValidatedUserAsync(request.Flow, request.Context,
            UAuthActions.Users.DeleteAdmin, "users", target, cleanup.Token);
        Assert.Same(validated, request.Flow);
        Assert.Equal(owner, rollback.ActorUserKey);
        Assert.Equal(target, rollback.TargetUserKey);
        Assert.Equal(UAuthActions.Users.DeleteAdmin, rollback.Action);
        Assert.Equal("users", rollback.Resource);
        Assert.Equal(request.Context.ActorTenant, rollback.ActorTenant);
        Assert.Equal(request.Context.ResourceTenant, rollback.ResourceTenant);
        Assert.True(rollback.IsAuthenticated);
        Assert.False(rollback.IsSystemActor);
        factory.Verify(x => x.CreateAsync(validated, UAuthActions.Users.DeleteAdmin, "users", target.Value, null, cleanup.Token), Times.Once);
        flows.Verify(x => x.BeginAsync(AuthFlowType.ValidateSession, It.IsAny<CancellationToken>()), Times.Exactly(2));
        flows.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("different-actor")]
    [InlineData("anonymous-actor")]
    [InlineData("system-actor")]
    [InlineData("different-tenant")]
    [InlineData("cross-tenant")]
    [InlineData("unauthenticated-flow")]
    [InlineData("missing-flow-actor")]
    public async Task Invalid_actor_or_flow_is_rejected_before_target_context_creation(string mismatch)
    {
        var flow = Contexts.AuthenticatedFlow(LabDatabase.OwnerA,
            authenticated: mismatch != "unauthenticated-flow", includeActor: mismatch != "missing-flow-actor");
        var actor = Contexts.Create(UAuthActions.Users.CreateAdmin,
            mismatch == "different-actor" ? LabDatabase.OwnerB : LabDatabase.OwnerA);
        actor = mismatch switch
        {
            "anonymous-actor" => Contexts.Create(UAuthActions.Users.CreateAdmin),
            "system-actor" => Contexts.Create(UAuthActions.Users.CreateAdmin, LabDatabase.OwnerA, system: true),
            "different-tenant" => Contexts.Create(UAuthActions.Users.CreateAdmin, LabDatabase.OwnerA, actorTenant: TenantKey.FromExternal("other"), resourceTenant: TenantKey.FromExternal("other")),
            "cross-tenant" => Contexts.Create(UAuthActions.Users.CreateAdmin, LabDatabase.OwnerA, resourceTenant: TenantKey.FromExternal("other")),
            _ => actor
        };
        var flows = new Mock<IAuthFlow>(MockBehavior.Strict);
        var factory = new Mock<IAccessContextFactory>(MockBehavior.Strict);
        var provider = new LabAccessContextProvider(flows.Object, factory.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => provider.CreateForValidatedUserAsync(
            flow, actor, UAuthActions.Users.DeleteAdmin, "users", UserKey.FromString("created-user"), TestContext.Current.CancellationToken));
        factory.VerifyNoOtherCalls();
        flows.VerifyNoOtherCalls();
    }
}

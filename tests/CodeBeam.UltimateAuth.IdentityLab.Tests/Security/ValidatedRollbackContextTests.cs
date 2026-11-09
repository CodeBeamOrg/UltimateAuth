using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Server.Auth;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Security;

public sealed class ValidatedRollbackContextTests
{
    [Fact]
    public async Task Retargeting_reuses_validated_flow_and_rejects_a_different_actor()
    {
        var owner = LabDatabase.OwnerA;
        var target = UserKey.FromString("created-user");
        var validated = Contexts.AuthenticatedFlow(owner);
        var flows = new Mock<IAuthFlow>(MockBehavior.Strict);
        flows.Setup(x => x.BeginAsync(AuthFlowType.ValidateSession, It.IsAny<CancellationToken>())).ReturnsAsync(validated);
        var factory = new Mock<IAccessContextFactory>(MockBehavior.Strict);
        factory.Setup(x => x.CreateAsync(validated, It.IsAny<string>(), "users", It.IsAny<string?>(), null, It.IsAny<CancellationToken>()))
            .Returns((AuthFlowContext _, string action, string resource, string? id, IDictionary<string, object>? attributes, CancellationToken ct) =>
                Task.FromResult(Contexts.Create(action, owner, id is null ? null : UserKey.FromString(id))));
        var provider = new LabAccessContextProvider(flows.Object, factory.Object);
        var context = await provider.CreateAsync(UAuthActions.Users.CreateAdmin, "users", TestContext.Current.CancellationToken);
        var rollback = await provider.CreateForValidatedUserAsync(context, UAuthActions.Users.DeleteAdmin, "users", target, TestContext.Current.CancellationToken);
        Assert.Equal(owner, rollback.ActorUserKey);
        Assert.Equal(target, rollback.TargetUserKey);
        Assert.True(rollback.IsAuthenticated);
        Assert.False(rollback.IsSystemActor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => provider.CreateForValidatedUserAsync(
            Contexts.Create(UAuthActions.Users.CreateAdmin, LabDatabase.OwnerB), UAuthActions.Users.DeleteAdmin,
            "users", target, TestContext.Current.CancellationToken));
        flows.Verify(x => x.BeginAsync(AuthFlowType.ValidateSession, It.IsAny<CancellationToken>()), Times.Once);
    }
}

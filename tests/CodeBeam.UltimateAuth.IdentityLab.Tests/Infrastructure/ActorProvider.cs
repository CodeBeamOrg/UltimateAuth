using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.Server.Auth;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Infrastructure;

internal static class ActorProvider
{
    public static LabAccessContextProvider For(UserKey? actor)
    {
        // Only the authenticated context source is replaced; application services,
        // ownership queries and PostgreSQL authorization decisions remain real.
        var flow = new Mock<IAuthFlow>();
        flow.Setup(x => x.BeginAsync(It.IsAny<AuthFlowType>(), It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<AuthFlowContext>((AuthFlowContext)null!));
        var contexts = new Mock<IAccessContextFactory>();
        contexts.Setup(x => x.CreateAsync(It.IsAny<AuthFlowContext>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IDictionary<string, object>?>(), It.IsAny<CancellationToken>()))
            .Returns((AuthFlowContext _, string action, string resource, string? id,
                IDictionary<string, object>? attributes, CancellationToken ct) =>
                Task.FromResult(Contexts.Create(action, actor, id is null ? null : UserKey.FromString(id))));
        return new LabAccessContextProvider(flow.Object, contexts.Object);
    }
}

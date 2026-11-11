using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class TryLoginTests
{
    [Fact]
    public async Task TryLogin_Should_Return_Preview_On_Failure()
    {
        var runtime = new TestAuthRuntime<string>();

        using var scope = runtime.Services.CreateScope();

        var flow = await runtime.CreateLoginFlowAsync();
        var orchestrator = scope.ServiceProvider.GetRequiredService<ILoginOrchestrator>();

        var result = await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Identifier = "user",
            Secret = "wrong"
        }, ct: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.RemainingAttempts.Should().BeNull();
        result.LockoutUntilUtc.Should().BeNull();
    }
}

using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server
{
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
            });

            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();

            result.RemainingAttempts.Should().NotBeNull();
        }
    }
}

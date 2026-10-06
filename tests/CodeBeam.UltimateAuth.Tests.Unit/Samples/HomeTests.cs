using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Sample.UAuthHub.Components.Layout;
using CodeBeam.UltimateAuth.Sample.UAuthHub.Components.Pages;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Tests.Unit.Samples.UAuthHub;

public sealed class HomeTests : UAuthHubComponentTestBase
{
    [Fact]
    public void Home_ShouldRender()
    {
        var state = UAuthState.Anonymous();

        var act = () => Render<CascadingValue<UAuthState>>(parameters => parameters
            .Add(p => p.Value, state)
            .AddChildContent<Home>());

        act.Should().NotThrow();
    }
}

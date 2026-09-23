using Bunit;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthHubLayoutBaseTests : BunitContext
{
    private readonly Mock<IHubFlowReader> _reader = new();

    public UAuthHubLayoutBaseTests()
    {
        Services.AddSingleton(_reader.Object);
    }

    [Fact]
    public void Render_WithoutHubQuery_DoesNotReadHubState()
    {
        var cut = Render<TestHubLayout>();

        cut.Instance.CurrentHubState.Should().BeNull();
        cut.Instance.HasCurrentHub.Should().BeFalse();
        cut.Instance.HubActive.Should().BeFalse();
        cut.Instance.HubExpired.Should().BeFalse();
        cut.Instance.HubError.Should().BeNull();

        _reader.Verify(
            x => x.GetStateAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Render_WithInvalidHubQuery_DoesNotReadHubState()
    {
        Nav.NavigateTo("/login?uauth_hub=invalid");

        var cut = Render<TestHubLayout>();

        cut.Instance.CurrentHubState.Should().BeNull();

        _reader.Verify(
            x => x.GetStateAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void ResolveHubKey_UsesUAuthHubQueryParameter()
    {
        Nav.NavigateTo("/login?uauth_hub=my-hub-value&hub=legacy-value");

        var cut = Render<TestHubLayout>();

        cut.Instance.ResolvedHubKey
            .Should()
            .Be("my-hub-value");
    }

    private NavigationManager Nav =>
        Services.GetRequiredService<NavigationManager>();

    private sealed class TestHubLayout : UAuthHubLayoutBase
    {
        public HubFlowState? CurrentHubState => HubState;
        public bool HasCurrentHub => HasHub;
        public bool HubActive => IsHubActive;
        public bool HubExpired => IsExpired;
        public HubErrorCode? HubError => Error;

        public string? ResolvedHubKey => ResolveHubKey();

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.AddContent(0, Body);
        }
    }
}

using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthHubPageBaseTests : BunitContext
{
    private readonly Mock<IHubFlowReader> _reader = new();

    public UAuthHubPageBaseTests()
    {
        Services.AddSingleton(_reader.Object);
    }

    [Fact]
    public void Render_WithoutHubKey_DoesNotReadHubState()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();

        VerifyReaderNeverCalled();
    }

    [Fact]
    public void Render_WithInvalidHubKey_DoesNotReadHubState()
    {
        var state = UAuthState.Anonymous();

        NavigateWithHub("invalid-hub-key");

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();

        VerifyReaderNeverCalled();
    }

    [Fact]
    public void Render_WithLegacyHubQuery_DoesNotReadHubState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        Navigate($"/login?hub={hubId.Value}");

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();

        VerifyReaderNeverCalled();
    }

    [Fact]
    public void Render_WithValidHubKey_LoadsHubState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var hubState = CreateHubState(hubId);

        SetupHubState(hubId, hubState);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(hubState);
        page.HubActive.Should().BeTrue();

        VerifyReaderCalled(hubId, Times.Once());
    }

    [Fact]
    public void Render_WhenHubDoesNotExist_IsHubActiveFalse()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var hubState = CreateHubState(
            hubId,
            exists: false,
            active: true);

        SetupHubState(hubId, hubState);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(hubState);
        page.HubActive.Should().BeFalse();
    }

    [Fact]
    public void Render_WhenHubExistsButIsInactive_IsHubActiveFalse()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var hubState = CreateHubState(
            hubId,
            exists: true,
            active: false);

        SetupHubState(hubId, hubState);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(hubState);
        page.HubActive.Should().BeFalse();
    }

    [Fact]
    public void Render_WhenHubExistsAndIsActive_IsHubActiveTrue()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var hubState = CreateHubState(
            hubId,
            exists: true,
            active: true);

        SetupHubState(hubId, hubState);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.HubActive.Should().BeTrue();
    }

    [Fact]
    public void Render_PreservesCompleteHubState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var hubState = CreateHubState(
            hubId,
            exists: true,
            active: false,
            expired: true,
            completed: false,
            error: HubErrorCode.InvalidCredentials,
            attemptCount: 3,
            returnUrl: "/dashboard");

        SetupHubState(hubId, hubState);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(hubState);

        page.CurrentHubState!.Exists.Should().BeTrue();
        page.CurrentHubState.IsActive.Should().BeFalse();
        page.CurrentHubState.IsExpired.Should().BeTrue();
        page.CurrentHubState.IsCompleted.Should().BeFalse();
        page.CurrentHubState.Error.Should().Be(HubErrorCode.InvalidCredentials);
        page.CurrentHubState.AttemptCount.Should().Be(3);
        page.CurrentHubState.ReturnUrl.Should().Be("/dashboard");
    }

    [Fact]
    public async Task ReloadStateAsync_ReloadsCurrentHubState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var initial = CreateHubState(
            hubId,
            active: true);

        var updated = CreateHubState(
            hubId,
            active: false,
            expired: true);

        _reader
            .SetupSequence(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(initial)
            .ReturnsAsync(updated);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(initial);
        page.HubActive.Should().BeTrue();

        await page.ReloadStateAsync();

        page.CurrentHubState.Should().BeSameAs(updated);
        page.HubActive.Should().BeFalse();
        page.CurrentHubState!.IsExpired.Should().BeTrue();

        VerifyReaderCalled(hubId, Times.Exactly(2));
    }

    [Fact]
    public async Task ReloadStateAsync_WhenReaderReturnsNull_ClearsState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var initial = CreateHubState(hubId);

        _reader
            .SetupSequence(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(initial)
            .ReturnsAsync((HubFlowState?)null);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(initial);

        await page.ReloadStateAsync();

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();
    }

    [Fact]
    public async Task ReloadStateAsync_WithInvalidHubKey_ClearsExistingState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var initial = CreateHubState(hubId);

        SetupHubState(hubId, initial);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(initial);

        page.SetHubKey("invalid-hub-key");

        await page.ReloadStateAsync();

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();

        // Only the initial render should have reached the reader.
        VerifyReaderCalled(hubId, Times.Once());
    }

    [Fact]
    public async Task ReloadStateAsync_WithoutHubKey_ClearsExistingState()
    {
        var state = UAuthState.Anonymous();
        var hubId = HubSessionId.New();

        var initial = CreateHubState(hubId);

        SetupHubState(hubId, initial);

        NavigateWithHub(hubId);

        var cut = RenderPage(state);

        var page = GetPage(cut);

        page.CurrentHubState.Should().BeSameAs(initial);
        page.HubActive.Should().BeTrue();

        page.SetHubKey(null);

        await page.ReloadStateAsync();

        page.CurrentHubState.Should().BeNull();
        page.HubActive.Should().BeFalse();

        VerifyReaderCalled(hubId, Times.Once());
    }

    // -----------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------

    private void SetupHubState(
        HubSessionId hubId,
        HubFlowState state)
    {
        _reader
            .Setup(x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(state);
    }

    private static HubFlowState CreateHubState(
        HubSessionId hubId,
        bool exists = true,
        bool active = true,
        bool expired = false,
        bool completed = false,
        HubErrorCode? error = null,
        int attemptCount = 0,
        string? returnUrl = null)
    {
        return new HubFlowState
        {
            HubSessionId = hubId,
            Exists = exists,
            IsActive = active,
            IsExpired = expired,
            IsCompleted = completed,
            Error = error,
            AttemptCount = attemptCount,
            ReturnUrl = returnUrl
        };
    }

    private void NavigateWithHub(HubSessionId hubId)
    {
        NavigateWithHub(hubId.Value);
    }

    private void NavigateWithHub(string hubKey)
    {
        var uri = Nav.GetUriWithQueryParameter(
            UAuthConstants.Query.Hub,
            hubKey);

        Nav.NavigateTo(uri);
    }

    private void Navigate(string relativeUri)
    {
        Nav.NavigateTo(relativeUri);
    }

    private TestHubPage GetPage(
        IRenderedComponent<CascadingValue<UAuthState>> cut)
    {
        return cut.FindComponent<TestHubPage>().Instance;
    }

    private void VerifyReaderNeverCalled()
    {
        _reader.Verify(
            x => x.GetStateAsync(
                It.IsAny<HubSessionId>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private void VerifyReaderCalled(
        HubSessionId hubId,
        Times times)
    {
        _reader.Verify(
            x => x.GetStateAsync(
                hubId,
                It.IsAny<CancellationToken>()),
            times);
    }

    private IRenderedComponent<CascadingValue<UAuthState>>
        RenderPage(UAuthState state)
    {
        return Render<CascadingValue<UAuthState>>(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent<TestHubPage>());
    }

    private NavigationManager Nav =>
        Services.GetRequiredService<NavigationManager>();

    private sealed class TestHubPage : UAuthHubPageBase
    {
        public HubFlowState? CurrentHubState => HubState;

        public bool HubActive => IsHubActive;

        public void SetHubKey(string? value)
        {
            HubKey = value;
        }

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.AddContent(0, "hub-page");
        }
    }
}

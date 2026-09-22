using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthPageBaseTests : BunitContext
{
    [Fact]
    public void Render_WithoutQuery_UsesDefaultValues()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedPayload.Should().BeNull();
        page.ParsedReturnUrl.Should().BeNull();
        page.ParsedIdentifier.Should().BeNull();
        page.HasFocusRequest.Should().BeFalse();

        page.PayloadCallbackCount.Should().Be(0);
        page.FocusCallbackCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithFocusOne_InvokesFocusCallbackExactlyOnce()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_focus=1");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.FocusCallbackCount.Should().Be(1);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("yes")]
    [InlineData("2")]
    public void Render_WithNonOneFocus_DoesNotInvokeFocusCallback(
        string focus)
    {
        var state = UAuthState.Anonymous();

        Navigate($"/login?uauth_focus={focus}");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.FocusCallbackCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithReturnUrl_ParsesReturnUrl()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_return_url=%2Fdashboard%3Ftab%3Dsecurity");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedReturnUrl
            .Should()
            .Be("/dashboard?tab=security");
    }

    [Fact]
    public void Render_WithIdentifier_ParsesIdentifier()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_identifier=alice%40example.com");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedIdentifier.Should().Be("alice@example.com");
    }

    [Fact]
    public void Render_WithInvalidBase64Payload_DoesNotThrow()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth=%25%25%25invalid%25%25%25");

        var act = () => RenderPage<TestPage>(state);

        act.Should().NotThrow();
    }

    [Fact]
    public void Render_WithInvalidBase64Payload_LeavesPayloadNull()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth=%25%25%25invalid%25%25%25");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedPayload.Should().BeNull();
        page.PayloadCallbackCount.Should().Be(0);
    }

    [Fact]
    public void Render_WithValidBase64ButInvalidJson_LeavesPayloadNull()
    {
        var state = UAuthState.Anonymous();

        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders
            .Base64UrlEncode(
                System.Text.Encoding.UTF8.GetBytes("not-json"));

        Navigate($"/login?uauth={encoded}");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedPayload.Should().BeNull();
        page.PayloadCallbackCount.Should().Be(0);
    }

    [Fact]
    public void Render_DefaultBehavior_ClearsQueryAfterProcessing()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_identifier=alice%40example.com&uauth_focus=1");

        RenderPage<TestPage>(state);

        Nav.Uri.Should().Be("http://localhost/login");
    }

    [Fact]
    public void Render_WhenClearQueryDisabled_PreservesQuery()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_identifier=alice%40example.com&uauth_focus=1");

        RenderPage<PersistentQueryPage>(state);

        Nav.Uri.Should()
            .Contain("uauth_identifier=alice%40example.com");

        Nav.Uri.Should()
            .Contain("uauth_focus=1");
    }

    [Fact]
    public void Render_QueryCleanup_PreservesPath()
    {
        var state = UAuthState.Anonymous();

        Navigate("/account/security/login?uauth_focus=1");

        RenderPage<TestPage>(state);

        Nav.Uri.Should()
            .Be("http://localhost/account/security/login");
    }

    [Fact]
    public void Render_WithFocusAndQueryCleanup_StillInvokesFocusBeforeCleanup()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_focus=1");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.FocusCallbackCount.Should().Be(1);
        Nav.Uri.Should().Be("http://localhost/login");
    }

    [Fact]
    public void Render_WithIdentifierAndQueryCleanup_ParsesValueBeforeCleanup()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?uauth_identifier=alice%40example.com");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedIdentifier.Should().Be("alice@example.com");
        Nav.Uri.Should().Be("http://localhost/login");
    }

    [Fact]
    public void Render_QueryCleanup_PreservesUnrelatedQueryParameters()
    {
        var state = UAuthState.Anonymous();

        Navigate(
            "/login?uauth_focus=1&culture=tr-TR&theme=dark");

        RenderPage<TestPage>(state);

        Nav.Uri.Should().Contain("culture=tr-TR");
        Nav.Uri.Should().Contain("theme=dark");
        Nav.Uri.Should().NotContain("uauth_focus");
    }

    [Fact]
    public void Render_WithOnlyUnrelatedQuery_DoesNotClearQuery()
    {
        var state = UAuthState.Anonymous();

        Navigate("/login?culture=tr-TR&theme=dark");

        RenderPage<TestPage>(state);

        Nav.Uri.Should()
            .Be("http://localhost/login?culture=tr-TR&theme=dark");
    }

    [Fact]
    public void Render_QueryCleanup_RemovesAllConsumedUAuthParameters()
    {
        var state = UAuthState.Anonymous();

        Navigate(
            "/login?uauth_focus=1" +
            "&uauth_identifier=alice" +
            "&uauth_return_url=%2Fdashboard" +
            "&culture=tr-TR");

        RenderPage<TestPage>(state);

        Nav.Uri.Should().Be(
            "http://localhost/login?culture=tr-TR");
    }

    [Fact]
    public void Render_QueryCleanup_PreservesMultipleValuesOfUnrelatedParameter()
    {
        var state = UAuthState.Anonymous();

        Navigate(
            "/login?uauth_focus=1&tag=one&tag=two");

        RenderPage<TestPage>(state);

        Nav.Uri.Should().Contain("tag=one");
        Nav.Uri.Should().Contain("tag=two");
        Nav.Uri.Should().NotContain("uauth_focus");
    }

    [Fact]
    public void Render_WithValidPayload_ParsesPayload()
    {
        var state = UAuthState.Anonymous();
        var payload = CreatePayload();
        var encoded = EncodePayload(payload);

        Navigate($"/login?uauth={encoded}");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.ParsedPayload.Should().NotBeNull();
        page.LastPayload.Should().NotBeNull();
    }

    [Fact]
    public void Render_WithValidPayload_InvokesPayloadCallbackExactlyOnce()
    {
        var state = UAuthState.Anonymous();
        var payload = CreatePayload();
        var encoded = EncodePayload(payload);

        Navigate($"/login?uauth={encoded}");

        var cut = RenderPage<TestPage>(state);

        var page = cut.FindComponent<TestPage>().Instance;

        page.PayloadCallbackCount.Should().Be(1);
    }

    [Fact]
    public void AuthStateRerender_DoesNotConsumeSamePayloadAgain()
    {
        var state = UAuthState.Anonymous();
        var payload = CreatePayload();
        var encoded = EncodePayload(payload);

        Navigate($"/login?uauth={encoded}");

        var cut = RenderPage<PersistentQueryPage>(state);

        var page = cut
            .FindComponent<PersistentQueryPage>()
            .Instance;

        page.PayloadCallbackCount.Should().Be(1);

        state.Touch();

        cut.WaitForAssertion(() =>
        {
            page.PayloadCallbackCount.Should().Be(1);
        });
    }

    [Fact]
    public void NewUri_AllowsNewPayloadToBeConsumed()
    {
        var state = UAuthState.Anonymous();

        var firstPayload = CreatePayload(
            status: "failed",
            reason: AuthFailureReason.InvalidCredentials,
            remainingAttempts: 2);

        var firstEncoded = EncodePayload(firstPayload);

        Navigate($"/login?uauth={firstEncoded}");

        var cut = RenderPage<PersistentQueryPage>(state);

        var page = cut
            .FindComponent<PersistentQueryPage>()
            .Instance;

        page.PayloadCallbackCount.Should().Be(1);
        page.LastPayload!.Status.Should().Be("failed");

        var secondPayload = CreatePayload(
            status: "locked",
            reason: AuthFailureReason.LockedOut,
            remainingAttempts: 0);

        var secondEncoded = EncodePayload(secondPayload);

        Navigate($"/login?uauth={secondEncoded}");

        cut.Render(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent<PersistentQueryPage>());

        page = cut
            .FindComponent<PersistentQueryPage>()
            .Instance;

        page.PayloadCallbackCount.Should().Be(2);

        page.LastPayload.Should().NotBeNull();
        page.LastPayload!.Status.Should().Be("locked");
        page.LastPayload.Reason.Should().Be(AuthFailureReason.LockedOut);
    }

    [Fact]
    public void Render_WithPayloadAndFocus_ConsumesBothExactlyOnce()
    {
        var state = UAuthState.Anonymous();
        var payload = CreatePayload();
        var encoded = EncodePayload(payload);

        Navigate(
            $"/login?uauth={encoded}&uauth_focus=1");

        var cut = RenderPage<PersistentQueryPage>(state);

        var page = cut
            .FindComponent<PersistentQueryPage>()
            .Instance;

        page.PayloadCallbackCount.Should().Be(1);
        page.FocusCallbackCount.Should().Be(1);

        cut.InvokeAsync(page.RequestRender);

        cut.WaitForAssertion(() =>
        {
            page.PayloadCallbackCount.Should().Be(1);
            page.FocusCallbackCount.Should().Be(1);
        });
    }

    [Fact]
    public void Rerender_DoesNotConsumeSamePayloadAgain()
    {
        var state = UAuthState.Anonymous();
        var payload = CreatePayload();
        var encoded = EncodePayload(payload);

        Navigate($"/login?uauth={encoded}");

        var cut = RenderPage<PersistentQueryPage>(state);

        var page = cut
            .FindComponent<PersistentQueryPage>()
            .Instance;

        page.PayloadCallbackCount.Should().Be(1);

        cut.InvokeAsync(page.RequestRender);

        cut.WaitForAssertion(() =>
        {
            page.PayloadCallbackCount.Should().Be(1);
        });
    }

    private void Navigate(string relativeUri)
    {
        Nav.NavigateTo(relativeUri);
    }

    private NavigationManager Nav =>
        Services.GetRequiredService<NavigationManager>();

    private IRenderedComponent<CascadingValue<UAuthState>>
        RenderPage<TPage>(UAuthState state)
        where TPage : IComponent
    {
        return Render<CascadingValue<UAuthState>>(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent<TPage>());
    }

    private static AuthFlowPayload CreatePayload(
        AuthFlowType flow = AuthFlowType.Login,
        string status = "failed",
        AuthFailureReason? reason = AuthFailureReason.InvalidCredentials,
        int? remainingAttempts = 3,
        DateTimeOffset? lockoutUntilUtc = null,
        int version = 1)
    {
        return new AuthFlowPayload
        {
            V = version,
            Flow = flow,
            Status = status,
            Reason = reason,
            RemainingAttempts = remainingAttempts,
            LockoutUntil = lockoutUntilUtc?.ToUnixTimeSeconds()
        };
    }

    private static string EncodePayload(AuthFlowPayload payload)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        return Microsoft.AspNetCore.WebUtilities.WebEncoders
            .Base64UrlEncode(bytes);
    }

    private class TestPage : UAuthPageBase
    {
        public AuthFlowPayload? ParsedPayload => UAuthPayload;
        public string? ParsedReturnUrl => ReturnUrl;
        public string? ParsedIdentifier => Identifier;
        public bool HasFocusRequest => ShouldFocus;

        public int PayloadCallbackCount { get; private set; }
        public int FocusCallbackCount { get; private set; }

        public AuthFlowPayload? LastPayload { get; private set; }

        protected override Task OnUAuthPayloadAsync(
            AuthFlowPayload payload)
        {
            PayloadCallbackCount++;
            LastPayload = payload;

            return Task.CompletedTask;
        }

        protected override Task OnFocusRequestedAsync()
        {
            FocusCallbackCount++;

            return Task.CompletedTask;
        }

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.AddContent(0, "uauth-test-page");
        }

        public void RequestRender()
        {
            StateHasChanged();
        }
    }

    private sealed class PersistentQueryPage : TestPage
    {
        protected override bool ClearUAuthQueryAfterParse => false;
    }
}

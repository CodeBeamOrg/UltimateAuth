using AwesomeAssertions;
using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthLoginFormContextTests : BunitContext
{
    private readonly Mock<IHubCredentialResolver> _credentials = new();
    private readonly Mock<IHubFlowReader> _flows = new();
    private readonly Mock<IHubCapabilities> _capabilities = new();

    public UAuthLoginFormContextTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(Mock.Of<IDeviceIdProvider>());
        Services.AddSingleton(_credentials.Object);
        Services.AddSingleton(_flows.Object);
        Services.AddSingleton(_capabilities.Object);
        Services.AddSingleton<IOptions<UAuthClientOptions>>(Options.Create(new UAuthClientOptions()));
    }

    [Fact]
    public void Default_Form_Renders_Exactly_One_Native_Identifier_And_Secret_And_Submit()
    {
        var cut = Render<UAuthLoginForm>();
        var form = cut.Find("form");

        form.GetAttribute("method").Should().Be("post");
        cut.FindAll("input[name='Identifier']").Should().ContainSingle();
        cut.FindAll("input[name='Secret']").Should().ContainSingle();
        cut.Find("input[name='Secret']").GetAttribute("type").Should().Be("password");
        cut.Find("input[name='Secret']").GetAttribute("autocomplete").Should().Be("current-password");
        cut.FindAll("button[type='submit']").Should().ContainSingle();
        cut.Find("button[type='submit']").HasAttribute("hidden").Should().BeFalse();
    }

    [Fact]
    public void Custom_Content_Replaces_Default_Fields_Without_Duplicates()
    {
        var cut = Render<UAuthLoginForm>(p => p.Add(x => x.ChildContent, (RenderFragment)(builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "custom-login");
            builder.OpenElement(2, "input");
            builder.AddAttribute(3, "name", "Identifier");
            builder.CloseElement();
            builder.OpenElement(4, "input");
            builder.AddAttribute(5, "name", "Secret");
            builder.AddAttribute(6, "type", "password");
            builder.CloseElement();
            builder.CloseElement();
        })));

        cut.FindAll(".custom-login").Should().ContainSingle();
        cut.FindAll("input[name='Identifier']").Should().ContainSingle();
        cut.FindAll("input[name='Secret']").Should().ContainSingle();
        cut.FindAll(".uauth-login-fields").Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void Custom_Content_Respects_Enter_Submit_Fallback(bool allowEnter, int expectedButtons)
    {
        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.AllowEnterKeyToSubmit, allowEnter)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddMarkupContent(0, "<input name='Identifier' /><input name='Secret' />"))));

        cut.FindAll("button[type='submit']").Should().HaveCount(expectedButtons);
    }

    [Fact]
    public void Default_Form_Does_Not_Expose_Credential_Values_As_Component_Parameters()
    {
        var parameters = typeof(UAuthLoginForm).GetProperties()
            .Where(p => p.IsDefined(typeof(ParameterAttribute), inherit: true))
            .Select(p => p.Name);

        parameters.Should().NotContain("Identifier").And.NotContain("Secret");
    }

    [Fact]
    public void Form_Renders_Metadata_And_Explicit_ReturnUrl()
    {
        var cut = Render<UAuthLoginForm>(p => p.Add(x => x.ReturnUrl, "/dashboard"));

        cut.FindAll($"input[name='{UAuthConstants.Form.ClientProfile}']").Should().ContainSingle();
        cut.FindAll($"input[name='{UAuthConstants.Form.Device}']").Should().ContainSingle();
        cut.Find($"input[name='{UAuthConstants.Form.ReturnUrl}']")
            .GetAttribute("value").Should().Be("/dashboard");
        cut.Find("form").GetAttribute("action").Should().Contain("uauth_return_url=%2Fdashboard");
    }

    [Fact]
    public void Pkce_Renders_Hub_Credentials_And_Hub_Query()
    {
        var hubId = HubSessionId.New();
        _capabilities.SetupGet(x => x.SupportsPkce).Returns(true);
        _credentials.Setup(x => x.ResolveAsync(hubId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HubCredentials { AuthorizationCode = "auth-code", CodeVerifier = "verifier" });

        var cut = Render<UAuthLoginForm>(p => p
            .Add(x => x.LoginType, UAuthLoginType.Pkce)
            .Add(x => x.HubSessionId, hubId));

        cut.Find("input[name='authorization_code']").GetAttribute("value").Should().Be("auth-code");
        cut.Find("input[name='code_verifier']").GetAttribute("value").Should().Be("verifier");
        cut.Find("input[name='hub_session_id']").GetAttribute("value").Should().Be(hubId.Value);
        cut.Find("form").GetAttribute("action").Should().Contain(Uri.EscapeDataString(hubId.Value));
    }

    [Fact]
    public void Pkce_Without_Hub_Support_Is_Rejected()
    {
        _capabilities.SetupGet(x => x.SupportsPkce).Returns(false);
        Action act = () => Render<UAuthLoginForm>(p => p.Add(x => x.LoginType, UAuthLoginType.Pkce));
        act.Should().Throw<InvalidOperationException>().WithMessage("*PKCE login requires UAuthHub*");
    }
}

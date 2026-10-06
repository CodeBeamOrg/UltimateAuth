using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Stores;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class HandleHubTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan FlowLifetime =
        TimeSpan.FromMinutes(10);

    private const string AuthorizationCode = "authorization-code";
    private const string CodeVerifier = "code-verifier";
    private const string DeviceIds = "device-id-123456789012345678901234567890";
    private const string ReturnUrl = "/dashboard";

    // ---------------------------------------------------------------------
    // Form requirement
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_WhenFormContentIsMissing_ShouldReturnBadRequest()
    {
        var fixture = CreateFixture();

        fixture.Context.Request.Method = HttpMethods.Post;

        var result = await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        result.Should().NotBeNull();

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // Artifact creation
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_ShouldCreateHubLoginArtifact()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        AuthArtifactKey? capturedKey = null;
        AuthArtifact? capturedArtifact = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (key, artifact, _) =>
                {
                    capturedKey = key;
                    capturedArtifact = artifact;
                })
            .Returns(Task.CompletedTask);

        await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        capturedKey.Should().NotBeNull();
        capturedArtifact.Should().BeOfType<HubFlowArtifact>();

        var flow = (HubFlowArtifact)capturedArtifact!;

        capturedKey!.Value.Should().Be(
            flow.HubSessionId.Value);

        flow.FlowType.Should().Be(
            HubFlowType.Login);

        flow.ClientProfile.Should().Be(
            UAuthClientProfile.BlazorWasm);

        flow.ReturnUrl.Should().Be(ReturnUrl);

        flow.IsCompleted.Should().BeFalse();
        flow.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleHubEntry_ShouldStorePkceCredentialsInPayload()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        HubFlowArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured = artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        captured.Should().NotBeNull();

        captured!.Payload
            .GetRequired<string>("authorization_code")
            .Should().Be(AuthorizationCode);

        captured.Payload
            .GetRequired<string>("code_verifier")
            .Should().Be(CodeVerifier);
    }

    [Fact]
    public async Task HandleHubEntry_ShouldSetExpirationFromClockAndConfiguredLifetime()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        HubFlowArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured = artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        captured.Should().NotBeNull();

        captured!.ExpiresAt.Should().Be(
            Now.Add(FlowLifetime));
    }

    // ---------------------------------------------------------------------
    // Client profile
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_ShouldParseClientProfileCaseInsensitively()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form["__uauth_client_profile"] =
            "blazorwasm";

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        artifact.ClientProfile.Should().Be(
            UAuthClientProfile.BlazorWasm);
    }

    [Fact]
    public async Task HandleHubEntry_WhenClientProfileIsInvalid_ShouldUseNotSpecified()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form["__uauth_client_profile"] =
            "definitely-not-a-client-profile";

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        artifact.ClientProfile.Should().Be(
            UAuthClientProfile.NotSpecified);
    }

    [Fact]
    public async Task HandleHubEntry_WhenClientProfileIsMissing_ShouldUseNotSpecified()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form.Remove("__uauth_client_profile");

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        artifact.ClientProfile.Should().Be(
            UAuthClientProfile.NotSpecified);
    }

    // ---------------------------------------------------------------------
    // Device
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_WhenDeviceIsMissing_ShouldUseAnonymousDevice()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form.Remove("device");

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        AssertAnonymousDevice(artifact.Device);
    }

    [Fact]
    public async Task HandleHubEntry_WhenDeviceIsEmpty_ShouldUseAnonymousDevice()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form["device"] = "   ";

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        AssertAnonymousDevice(artifact.Device);
    }

    [Fact]
    public async Task HandleHubEntry_WhenDeviceIsInvalidBase64_ShouldUseAnonymousDevice()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form["device"] = "%%%not-valid-base64%%%";

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        AssertAnonymousDevice(artifact.Device);
    }

    [Fact]
    public async Task HandleHubEntry_WhenDeviceJsonIsInvalid_ShouldUseAnonymousDevice()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        var invalidJson =
            Microsoft.AspNetCore.WebUtilities.WebEncoders
                .Base64UrlEncode(
                    System.Text.Encoding.UTF8.GetBytes(
                        "{ definitely-invalid-json"));

        form["device"] = invalidJson;

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        AssertAnonymousDevice(artifact.Device);
    }

    // ---------------------------------------------------------------------
    // Return URL
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_ShouldPreserveReturnUrl()
    {
        var fixture = CreateFixture();

        var form = CreateForm();

        form[UAuthConstants.Form.ReturnUrl] =
            "/orders/42?tab=details";

        SetForm(fixture.Context, form);

        var artifact = await ExecuteAndCaptureArtifact(fixture);

        artifact.ReturnUrl.Should().Be(
            "/orders/42?tab=details");
    }

    // ---------------------------------------------------------------------
    // Store
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_ShouldStoreArtifactUsingGeneratedHubSessionId()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        AuthArtifactKey? capturedKey = null;
        HubFlowArtifact? capturedArtifact = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (key, artifact, _) =>
                {
                    capturedKey = key;
                    capturedArtifact =
                        artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        capturedKey.Should().NotBeNull();
        capturedArtifact.Should().NotBeNull();

        capturedKey!.Value.Should().Be(
            capturedArtifact!.HubSessionId.Value);
    }

    // ---------------------------------------------------------------------
    // Redirect
    // ---------------------------------------------------------------------

    [Fact]
    public async Task HandleHubEntry_ShouldReturnRedirectToConfiguredLoginPath()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        var result = await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        var httpResult =
            result.Should()
                .BeAssignableTo<IResult>()
                .Subject;

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        // Execute the IResult so we can inspect the real HTTP boundary.
        fixture.Context.Response.Body =
            new MemoryStream();

        await httpResult.ExecuteAsync(
            fixture.Context);

        fixture.Context.Response.StatusCode
            .Should().Be(StatusCodes.Status302Found);

        fixture.Context.Response.Headers.Location
            .ToString()
            .Should()
            .StartWith(
                fixture.Options.Value.Hub.LoginPath);

        fixture.Context.Response.Headers.Location
            .ToString()
            .Should()
            .Contain($"{UAuthConstants.Query.Hub}=");
    }

    [Fact]
    public async Task HandleHubEntry_RedirectHubId_ShouldMatchStoredArtifact()
    {
        var fixture = CreateFixture();

        SetForm(
            fixture.Context,
            CreateForm());

        HubFlowArtifact? capturedArtifact = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    capturedArtifact =
                        artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        var result = await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        capturedArtifact.Should().NotBeNull();

        fixture.Context.Response.Body =
            new MemoryStream();

        await result.ExecuteAsync(
            fixture.Context);

        var location =
            fixture.Context.Response.Headers.Location
                .ToString();

        location.Should().Contain(
            $"{UAuthConstants.Query.Hub}=" +
            $"{capturedArtifact!.HubSessionId.Value}");
    }

    [Fact]
    public async Task HandleHubEntry_WhenDeviceIsValid_ShouldPreserveDeviceContext()
    {
        var fixture = CreateFixture();

        var deviceId = DeviceId.Create(DeviceIds);

        var device = DeviceContext.Create(
            deviceId,
            deviceType: "Desktop",
            platform: "Web",
            operatingSystem: "Windows",
            browser: "Edge",
            ipAddress: "127.0.0.1");

        var json =
            System.Text.Json.JsonSerializer.Serialize(device);

        var encoded =
            Microsoft.AspNetCore.WebUtilities.WebEncoders
                .Base64UrlEncode(
                    System.Text.Encoding.UTF8.GetBytes(json));

        var form = CreateForm();
        form["device"] = encoded;

        SetForm(fixture.Context, form);

        var artifact =
            await ExecuteAndCaptureArtifact(fixture);

        artifact.Device.DeviceId.Should().Be(deviceId);
        artifact.Device.HasDeviceId.Should().BeTrue();

        artifact.Device.DeviceType.Should().Be("desktop");
        artifact.Device.Platform.Should().Be("web");
        artifact.Device.OperatingSystem.Should().Be("windows");
        artifact.Device.Browser.Should().Be("edge");
        artifact.Device.IpAddress.Should().Be("127.0.0.1");
    }

    // ---------------------------------------------------------------------
    // Fixture
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture()
    {
        var store = new Mock<IAuthStore>();

        store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clock = new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var options = new UAuthServerOptions();

        options.Hub.FlowLifetime = FlowLifetime;
        options.Hub.LoginPath = "/auth/login";

        var context = new DefaultHttpContext();

        context.Items[UAuthConstants.HttpItems.TenantContextKey] = UAuthTenantContext.SingleTenant();

        // Required for executing Results.Redirect().
        context.RequestServices =
            new Microsoft.Extensions.DependencyInjection
                .ServiceCollection()
                .AddLogging()
                .BuildServiceProvider();

        return new Fixture(
            context,
            store,
            clock,
            Options.Create(options));
    }

    private static Dictionary<string, StringValues> CreateForm()
    {
        return new Dictionary<string, StringValues>
        {
            ["authorization_code"] =
                AuthorizationCode,

            ["code_verifier"] =
                CodeVerifier,

            ["device_id"] =
                DeviceIds,

            [UAuthConstants.Form.ReturnUrl] =
                ReturnUrl,

            ["__uauth_client_profile"] =
                UAuthClientProfile.BlazorWasm.ToString()
        };
    }

    private static void SetForm(
        HttpContext context,
        Dictionary<string, StringValues> values)
    {
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType =
            "application/x-www-form-urlencoded";

        context.Request.Form =
            new FormCollection(values);
    }

    private static async Task<HubFlowArtifact> ExecuteAndCaptureArtifact(Fixture fixture)
    {
        HubFlowArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured =
                        artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await HandleHub.HandleHubEntry(
            fixture.Context,
            fixture.Store.Object,
            fixture.Clock.Object,
            fixture.Options);

        captured.Should().NotBeNull();

        return captured!;
    }

    private static void AssertAnonymousDevice(
    DeviceContext device)
    {
        device.Should().NotBeNull();

        device.DeviceId.Should().BeNull();
        device.HasDeviceId.Should().BeFalse();

        device.DeviceType.Should().BeNull();
        device.Platform.Should().BeNull();
        device.OperatingSystem.Should().BeNull();
        device.Browser.Should().BeNull();
        device.IpAddress.Should().BeNull();
    }

    private sealed record Fixture(
        DefaultHttpContext Context,
        Mock<IAuthStore> Store,
        Mock<IClock> Clock,
        IOptions<UAuthServerOptions> Options);
}

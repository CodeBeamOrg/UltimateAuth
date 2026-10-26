using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthResourceRequestClientTests
{
    private readonly Mock<IJSRuntime> _js = new();
    private readonly Mock<IUAuthClientBootstrapper> _bootstrapper = new();

    private UAuthResourceRequestClient CreateClient(
        string? baseAddress = "https://localhost:6120",
        UAuthResourceCredentialsMode credentials =
            UAuthResourceCredentialsMode.Auto)
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Resource = new UAuthClientResourceOptions
            {
                BaseAddress = baseAddress,
                Credentials = credentials
            }
        });

        return new UAuthResourceRequestClient(
            _js.Object,
            _bootstrapper.Object,
            options);
    }

    private void SetupResponse(
        UAuthResourceTransportResult? response)
    {
        _js.Setup(x => x.InvokeAsync<UAuthResourceTransportResult>(
                "uauth.resourceRequest",
                It.IsAny<CancellationToken>(),
                It.IsAny<object?[]>()))
            .ReturnsAsync(response!);
    }

    [Fact]
    public async Task Send_Should_Ensure_Bootstrap_Before_JS_Request()
    {
        var bootstrapCompleted =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _bootstrapper
            .Setup(x => x.EnsureStartedAsync(
                It.IsAny<CancellationToken>()))
            .Returns(bootstrapCompleted.Task);

        SetupResponse(new UAuthResourceTransportResult
        {
            Ok = true,
            Status = 200
        });

        var client = CreateClient();

        var task = client.SendAsync("GET", "/api/products");

        _bootstrapper.Verify(x => x.EnsureStartedAsync(
            It.IsAny<CancellationToken>()), Times.Once);

        _js.Verify(x => x.InvokeAsync<UAuthResourceTransportResult>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<object?[]>()), Times.Never);

        bootstrapCompleted.SetResult();

        await task;

        _js.Verify(x => x.InvokeAsync<UAuthResourceTransportResult>(
            "uauth.resourceRequest",
            It.IsAny<CancellationToken>(),
            It.IsAny<object?[]>()), Times.Once);
    }

    [Fact]
    public async Task Send_Should_Pass_Correct_Arguments_To_JS()
    {
        object? capturedOptions = null;

        _js.Setup(x => x.InvokeAsync<UAuthResourceTransportResult>(
                "uauth.resourceRequest",
                It.IsAny<CancellationToken>(),
                It.IsAny<object?[]>()))
            .Callback<string, CancellationToken, object?[]>(
                (_, _, args) => capturedOptions = args[0])
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 201
            });

        var payload = new { Name = "Product" };

        var client = CreateClient(
            "https://localhost:6120",
            UAuthResourceCredentialsMode.Include);

        await client.SendAsync(
            "POST",
            "/api/products",
            payload);

        capturedOptions.Should().NotBeNull();

        var type = capturedOptions!.GetType();

        type.GetProperty("url")!
            .GetValue(capturedOptions)
            .Should().Be("/api/products");

        type.GetProperty("method")!
            .GetValue(capturedOptions)
            .Should().Be("POST");

        type.GetProperty("baseAddress")!
            .GetValue(capturedOptions)
            .Should().Be("https://localhost:6120");

        type.GetProperty("credentials")!
            .GetValue(capturedOptions)
            .Should().Be("Include");

        type.GetProperty("payload")!
            .GetValue(capturedOptions)
            .Should().BeSameAs(payload);

        var requestId = type.GetProperty("requestId")!
            .GetValue(capturedOptions) as string;

        requestId.Should().NotBeNullOrWhiteSpace();
        Guid.TryParseExact(requestId, "N", out _)
            .Should().BeTrue();
    }

    [Fact]
    public async Task Send_Should_Return_Success_Response()
    {
        var response = new UAuthResourceTransportResult
        {
            Ok = true,
            Status = 200,
            Body = """{"id":1}""",
            ContentType = "application/json"
        };

        SetupResponse(response);

        var result = await CreateClient()
            .SendAsync("GET", "/api/products/1");

        result.Should().BeSameAs(response);
    }

    [Fact]
    public async Task Send_Should_Return_Http_Error_Response()
    {
        var response = new UAuthResourceTransportResult
        {
            Ok = false,
            Status = 403,
            Body = """{"title":"Forbidden"}"""
        };

        SetupResponse(response);

        var result = await CreateClient()
            .SendAsync("GET", "/api/products");

        result.Should().BeSameAs(response);
        result.Status.Should().Be(403);
        result.Ok.Should().BeFalse();
    }

    [Fact]
    public async Task Send_Should_Throw_When_JS_Returns_Null()
    {
        SetupResponse(null);

        var client = CreateClient();

        Func<Task> act = () =>
            client.SendAsync("GET", "/api/products");

        await act.Should()
            .ThrowAsync<UAuthProtocolException>();
    }

    [Fact]
    public async Task Send_Should_Throw_On_Network_Error()
    {
        SetupResponse(new UAuthResourceTransportResult
        {
            Ok = false,
            Status = 0
        });

        var client = CreateClient();

        Func<Task> act = () =>
            client.SendAsync("GET", "/api/products");

        await act.Should()
            .ThrowAsync<UAuthTransportException>();
    }

    [Fact]
    public async Task Send_Should_Not_Bootstrap_When_Already_Cancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var client = CreateClient();

        Func<Task> act = () =>
            client.SendAsync(
                "GET",
                "/api/products",
                ct: cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _bootstrapper.Verify(x => x.EnsureStartedAsync(
            It.IsAny<CancellationToken>()), Times.Never);

        _js.Verify(x => x.InvokeAsync<UAuthResourceTransportResult>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<object?[]>()), Times.Never);
    }

    [Fact]
    public async Task Send_Should_Not_Invoke_JS_When_Cancelled_During_Bootstrap()
    {
        var bootstrapCompleted =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        _bootstrapper
            .Setup(x => x.EnsureStartedAsync(
                It.IsAny<CancellationToken>()))
            .Returns(bootstrapCompleted.Task);

        using var cts = new CancellationTokenSource();

        var client = CreateClient();

        var task = client.SendAsync(
            "GET",
            "/api/products",
            ct: cts.Token);

        cts.Cancel();

        bootstrapCompleted.SetResult();

        Func<Task> act = () => task;

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        _js.Verify(x => x.InvokeAsync<UAuthResourceTransportResult>(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<object?[]>()), Times.Never);
    }

    [Fact]
    public async Task Send_Should_Propagate_JS_Cancellation()
    {
        using var cts = new CancellationTokenSource();

        _js.Setup(x => x.InvokeAsync<UAuthResourceTransportResult>(
                "uauth.resourceRequest",
                cts.Token,
                It.IsAny<object?[]>()))
            .Returns<string, CancellationToken, object?[]>(
                (_, ct, _) => new ValueTask<UAuthResourceTransportResult>(
                    Task.FromCanceled<UAuthResourceTransportResult>(ct)));

        var client = CreateClient();

        cts.Cancel();

        // The pre-cancelled token is rejected before JS invocation.
        Func<Task> act = () =>
            client.SendAsync(
                "GET",
                "/api/products",
                ct: cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Send_Should_Generate_Unique_RequestIds()
    {
        var requestIds = new List<string>();

        _js.Setup(x => x.InvokeAsync<UAuthResourceTransportResult>(
                "uauth.resourceRequest",
                It.IsAny<CancellationToken>(),
                It.IsAny<object?[]>()))
            .Callback<string, CancellationToken, object?[]>(
                (_, _, args) =>
                {
                    var options = args[0]!;
                    var requestId = options.GetType()
                        .GetProperty("requestId")!
                        .GetValue(options) as string;

                    requestIds.Add(requestId!);
                })
            .ReturnsAsync(new UAuthResourceTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateClient();

        await client.SendAsync("GET", "/api/products/1");
        await client.SendAsync("GET", "/api/products/2");

        requestIds.Should().HaveCount(2);
        requestIds.Distinct().Should().HaveCount(2);
    }
}

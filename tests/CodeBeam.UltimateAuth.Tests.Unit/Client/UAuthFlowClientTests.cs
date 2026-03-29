using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthFlowClientTests : UAuthClientTestBase
{
    private readonly Mock<IUAuthRequestClient> _mockRequest = new();

    private UAuthFlowClient CreateClient(Mock<IUAuthRequestClient>? requestMock = null)
    {
        var request = requestMock ?? new Mock<IUAuthRequestClient>();

        var events = new Mock<IUAuthClientEvents>();
        var deviceProvider = new Mock<IClientDeviceProvider>();
        var deviceIdProvider = new Mock<IDeviceIdProvider>();
        var returnUrlProvider = new Mock<IReturnUrlProvider>();

        deviceIdProvider
            .Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DeviceId>(
                DeviceId.Create("device-1234567890123456")));

        returnUrlProvider
            .Setup(x => x.GetCurrentUrl())
            .Returns("/home");

        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth",
                Login = "/login",
                TryLogin = "/try-login"
            },
            Login = new UAuthClientLoginFlowOptions
            {
                AllowCredentialPost = true
            }
        });

        var diagnostics = new UAuthClientDiagnostics();

        return new UAuthFlowClient(
            request.Object,
            events.Object,
            deviceProvider.Object,
            returnUrlProvider.Object,
            options,
            diagnostics);
    }

    private static UAuthTransportResult TryLoginResponse(bool success, AuthFailureReason? reason = null)
    {
        return new UAuthTransportResult
        {
            Status = 200,
            Body = JsonSerializer.SerializeToElement(new TryLoginResult
            {
                Success = success,
                Reason = reason
            })
        };
    }

    [Fact]
    public async Task TryLogin_Should_Call_TryLogin_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(TryLoginResponse(true));

        var client = CreateClient(mock);
        await client.TryLoginAsync(new LoginRequest { Identifier = "admin", Secret = "admin" }, UAuthSubmitMode.TryOnly);

        mock.Verify(x => x.SendJsonAsync("/auth/try-login", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task TryLogin_Should_Return_Success()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(TryLoginResponse(true));

        var client = CreateClient(mock);
        var result = await client.TryLoginAsync(new LoginRequest { Identifier = "admin", Secret = "admin" }, UAuthSubmitMode.TryOnly);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TryLogin_Should_Return_Failure()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(TryLoginResponse(false, AuthFailureReason.InvalidCredentials));

        var client = CreateClient(mock);
        var result = await client.TryLoginAsync(new LoginRequest { Identifier = "admin", Secret = "wrong" }, UAuthSubmitMode.TryOnly);

        result.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
    }

    [Fact]
    public async Task TryLogin_Should_Throw_When_Body_Null()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Body = null
            });

        var client = CreateClient(mock);
        Func<Task> act = async () => await client.TryLoginAsync(new LoginRequest(), UAuthSubmitMode.TryOnly);

        await act.Should().ThrowAsync<UAuthProtocolException>();
    }

    [Fact]
    public async Task TryLogin_Should_Throw_When_Invalid_Json()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Body = JsonDocument.Parse("\"invalid\"").RootElement
            });

        var client = CreateClient(mock);
        Func<Task> act = async () => await client.TryLoginAsync(new LoginRequest(), UAuthSubmitMode.TryOnly);

        await act.Should().ThrowAsync<UAuthProtocolException>();
    }

    [Fact]
    public async Task TryLogin_DirectCommit_Should_Navigate()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.NavigateAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(mock);

        var request = new LoginRequest
        {
            Identifier = "admin",
            Secret = "admin"
        };

        var result = await client.TryLoginAsync(request, UAuthSubmitMode.DirectCommit);
        result.Success.Should().BeTrue();

        mock.Verify(x => x.NavigateAsync("/auth/login",
            It.Is<IDictionary<string, string>>(d => d["Identifier"] == "admin" && d["Secret"] == "admin"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryLogin_TryAndCommit_Should_Call_TryAndCommit()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.TryAndCommitAsync<TryLoginResult>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .ReturnsAsync(new TryLoginResult { Success = true });

        var client = CreateClient(mock);
        var result = await client.TryLoginAsync(new LoginRequest(), UAuthSubmitMode.TryAndCommit);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TryLogin_Should_Throw_When_CredentialPost_Disabled()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Login = new UAuthClientLoginFlowOptions
            {
                AllowCredentialPost = false
            }
        });

        var mock = new Mock<IUAuthRequestClient>();

        var client = new UAuthFlowClient(
            mock.Object,
            Mock.Of<IUAuthClientEvents>(),
            Mock.Of<IClientDeviceProvider>(),
            Mock.Of<IReturnUrlProvider>(),
            options,
            new UAuthClientDiagnostics());

        Func<Task> act = async () => await client.TryLoginAsync(new LoginRequest(), UAuthSubmitMode.TryOnly);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Refresh_Should_Return_ReauthRequired_On_401()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 401
            });

        var client = CreateClient(mock);
        var result = await client.RefreshAsync();

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(RefreshOutcome.ReauthRequired);
    }

    [Fact]
    public async Task Refresh_Should_Return_Success()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = "success"
            });

        var client = CreateClient(mock);

        var result = await client.RefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Success);
    }

    [Fact]
    public async Task Validate_Should_Return_Result()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new AuthValidationResult
                {
                    State = SessionState.Active
                })
            });

        var client = CreateClient(mock);
        var result = await client.ValidateAsync();

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Login_Should_Navigate_To_Login_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFlowClient(mock);

        await client.LoginAsync(new LoginRequest
        {
            Identifier = "user",
            Secret = "pass"
        });

        mock.Verify(x => x.NavigateAsync("/auth/login",
                It.Is<IDictionary<string, string>>(d =>
                    d["Identifier"] == "user" &&
                    d["Secret"] == "pass"),
                It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_Should_Navigate_To_Logout()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.NavigateAsync(
            It.IsAny<string>(),
            null,
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFlowClient(mock);
        await client.LogoutAsync();
        mock.Verify(x => x.NavigateAsync("/auth/logout", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_Should_Not_Mark_Manual_When_Auto()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = "success"
            });

        var client = CreateFlowClient(mock);
        var result = await client.RefreshAsync(isAuto: true);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_Should_Publish_Event()
    {
        var request = new Mock<IUAuthRequestClient>();
        var events = new Mock<IUAuthClientEvents>();

        request.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new AuthValidationResult
                {
                    State = SessionState.Active
                })
            });

        var client = CreateFlowClient(request, events);
        await client.ValidateAsync();
        events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.ValidationCalled)), Times.Once);
    }

    [Fact]
    public async Task Validate_Should_Throw_On_Status_0()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 0
            });

        var client = CreateFlowClient(mock);
        Func<Task> act = async () => await client.ValidateAsync();
        await act.Should().ThrowAsync<UAuthTransportException>();
    }

    [Fact]
    public async Task LogoutMyDevice_Should_Publish_Event_When_CurrentChain()
    {
        var request = new Mock<IUAuthRequestClient>();
        var events = new Mock<IUAuthClientEvents>();

        request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new RevokeResult
                {
                    CurrentChain = true
                })
            });

        var client = CreateFlowClient(request, events);
        await client.LogoutMyDeviceAsync(new LogoutDeviceRequest() { ChainId = SessionChainId.New() });
        events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.LogoutVariant)), Times.Once);
    }

    [Fact]
    public async Task LogoutAllMyDevices_Should_Publish_Event_On_Success()
    {
        var request = new Mock<IUAuthRequestClient>();
        var events = new Mock<IUAuthClientEvents>();

        request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateFlowClient(request, events);
        await client.LogoutAllMyDevicesAsync();
        events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.LogoutVariant)), Times.Once);
    }

    [Fact]
    public async Task BeginPkce_Should_Throw_When_Disabled()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Pkce = new UAuthClientPkceLoginFlowOptions
            {
                Enabled = false
            }
        });

        var client = new UAuthFlowClient(
            Mock.Of<IUAuthRequestClient>(),
            Mock.Of<IUAuthClientEvents>(),
            Mock.Of<IClientDeviceProvider>(),
            Mock.Of<IReturnUrlProvider>(),
            options,
            new UAuthClientDiagnostics());

        Func<Task> act = async () => await client.BeginPkceAsync();
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BeginPkce_Should_Call_Authorize_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new PkceAuthorizeResponse
                {
                    AuthorizationCode = "code123"
                })
            });

        var client = CreateFlowClient(mock);
        await client.BeginPkceAsync();
        mock.Verify(x => x.SendFormAsync("/auth/pkce/authorize", It.Is<IDictionary<string, string>>(d =>
            d.ContainsKey("code_challenge") && d["challenge_method"] == "S256"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginPkce_Should_Throw_When_AuthorizationCode_Missing()
    {
        var mock = new Mock<IUAuthRequestClient>();

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
        mock.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new PkceAuthorizeResponse
                {
                    AuthorizationCode = null
                })
            });
#pragma warning restore CS8625

        var client = CreateFlowClient(mock);

        Func<Task> act = async () => await client.BeginPkceAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Invalid PKCE authorize response*");
    }

    [Fact]
    public async Task BeginPkce_Should_AutoRedirect_When_Enabled()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new PkceAuthorizeResponse
                {
                    AuthorizationCode = "code123"
                })
            });

        mock.Setup(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth",
                PkceAuthorize = "/pkce/authorize",
                HubLoginPath = "/hub/login"
            },
            Pkce = new UAuthClientPkceLoginFlowOptions
            {
                Enabled = true,
                AutoRedirect = true
            }
        });

        var client = new UAuthFlowClient(
            mock.Object,
            Mock.Of<IUAuthClientEvents>(),
            Mock.Of<IClientDeviceProvider>(),
            Mock.Of<IReturnUrlProvider>(),
            options,
            new UAuthClientDiagnostics());

        await client.BeginPkceAsync();
        mock.Verify(x => x.NavigateAsync("/auth/hub/login", It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryCompletePkce_Should_Call_Try_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new TryPkceLoginResult
                {
                    Success = true
                })
            });

        var client = CreateFlowClient(mock);

        var result = await client.TryCompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                AuthorizationCode = "code",
                CodeVerifier = "verifier",
                Identifier = "user",
                Secret = "pass"
            },
            UAuthSubmitMode.TryOnly);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TryCompletePkce_Should_Call_TryAndCommit()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.TryAndCommitAsync<TryPkceLoginResult>(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<object>()))
            .ReturnsAsync(new TryPkceLoginResult { Success = true });

        var client = CreateFlowClient(mock);

        var result = await client.TryCompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                AuthorizationCode = "code",
                CodeVerifier = "verifier",
                Identifier = "user",
                Secret = "pass"
            },
            UAuthSubmitMode.TryAndCommit);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TryCompletePkce_DirectCommit_Should_Navigate()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFlowClient(mock);

        var result = await client.TryCompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                AuthorizationCode = "code",
                CodeVerifier = "verifier",
                Identifier = "user",
                Secret = "pass"
            },
            UAuthSubmitMode.DirectCommit);

        result.Success.Should().BeTrue();

        mock.Verify(x =>
            x.NavigateAsync("/auth/pkce/complete",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompletePkce_Should_Navigate_With_Payload()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFlowClient(mock);

        await client.CompletePkceLoginAsync(new PkceCompleteRequest
        {
            AuthorizationCode = "code",
            CodeVerifier = "verifier",
            ReturnUrl = "/home",
            Identifier = "user",
            Secret = "pass"
        });

        mock.Verify(x =>
            x.NavigateAsync("/auth/pkce/complete",
                It.Is<IDictionary<string, string>>(d =>
                    d["authorization_code"] == "code" &&
                    d["code_verifier"] == "verifier"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
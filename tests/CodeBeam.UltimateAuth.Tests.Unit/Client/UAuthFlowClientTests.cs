using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
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
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthFlowClientTests : UAuthClientTestBase
{
    private readonly Mock<IUAuthRequestClient> _mockRequest = new();

    private UAuthFlowClient CreateClient(
        Mock<IUAuthRequestClient>? requestMock = null,
        UAuthClientDiagnostics? diagnostics = null,
        IUAuthTokenStore? tokenStore = null,
        IUAuthClientEvents? clientEvents = null)
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

        var actualEvents = clientEvents ?? events.Object;
        var actualDiagnostics = diagnostics ?? new UAuthClientDiagnostics();
        var actualStore = tokenStore ?? new InMemoryUAuthTokenStore();

        var refreshCoordinator = CreateRefreshCoordinator(
            request.Object,
            actualEvents,
            actualStore,
            actualDiagnostics,
            "/auth/refresh");

        return new UAuthFlowClient(
            request.Object,
            actualEvents,
            deviceProvider.Object,
            returnUrlProvider.Object,
            options,
            actualStore,
            refreshCoordinator);
    }

    private static AccessToken CreateStoredToken()
    {
        return new AccessToken
        {
            Token = "existing-jwt",
            Format = TokenFormat.Jwt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    private static UAuthTransportResult TryLoginResponse(bool success, AuthFailureReason? reason = null)
    {
        return new UAuthTransportResult
        {
            Status = 200,
            Body = JsonSerializer.SerializeToElement(new TryLoginResult
            {
                IsSuccess = success,
                Reason = reason
            })
        };
    }

    private static Mock<IUAuthRequestClient> CreatePkceFetchRequestMock(
    UAuthTransportResult response)
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.GetLoginTransportAsync(
                "/auth/login/transport",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = false,
                AccessTokenInHeader = true
            });

        mock.Setup(x => x.SendFormAsync(
                "/auth/pkce/complete",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        return mock;
    }

    private static UAuthTransportResult CreatePkceFetchResponse(
        string? redirectUrl = null)
    {
        return new UAuthTransportResult
        {
            Ok = true,
            Status = 200,
            AccessToken = "Bearer pkce-jwt",
            AccessTokenFormat = "jwt",
            AccessTokenHeaderFormat = "bearer",
            AccessTokenExpiresAt =
                DateTimeOffset.UtcNow.AddMinutes(10).ToString("O"),
            Body = JsonSerializer.SerializeToElement(new
            {
                redirectUrl
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

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TryLogin_Should_Return_Failure()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(TryLoginResponse(false, AuthFailureReason.InvalidCredentials));

        var client = CreateClient(mock);
        var result = await client.TryLoginAsync(new LoginRequest { Identifier = "admin", Secret = "wrong" }, UAuthSubmitMode.TryOnly);

        result.IsSuccess.Should().BeFalse();
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

        mock.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = true,
                AccessTokenInHeader = false
            });

        mock.Setup(x => x.NavigateAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(mock);

        var result = await client.TryLoginAsync(
            new LoginRequest
            {
                Identifier = "admin",
                Secret = "admin"
            },
            UAuthSubmitMode.DirectCommit,
            transport: UAuthLoginTransport.Navigation);

        result.IsSuccess.Should().BeTrue();

        mock.Verify(x => x.NavigateAsync(
            "/auth/login",
            It.Is<IDictionary<string, string>>(d =>
                d["Identifier"] == "admin" &&
                d["Secret"] == "admin"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryLogin_TryAndCommit_Should_Try_Then_Commit()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(
                "/auth/try-login",
                It.IsAny<object>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(TryLoginResponse(true));

        mock.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = true,
                AccessTokenInHeader = false
            });

        mock.Setup(x => x.NavigateAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(mock);

        var result = await client.TryLoginAsync(
            new LoginRequest
            {
                Identifier = "admin",
                Secret = "admin"
            },
            UAuthSubmitMode.TryAndCommit,
            transport: UAuthLoginTransport.Navigation);

        result.IsSuccess.Should().BeTrue();

        mock.Verify(x => x.SendJsonAsync(
            "/auth/try-login",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()), Times.Once);

        mock.Verify(x => x.NavigateAsync(
            "/auth/login",
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
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
            new InMemoryUAuthTokenStore(),
            Mock.Of<IUAuthRefreshCoordinator>());

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
            It.IsAny<IDictionary<string, string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new UAuthTransportResult
        {
            Status = 200,
            Body = JsonSerializer.SerializeToElement(
                new AuthValidationResult
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

        mock.Setup(x => x.GetLoginTransportAsync(
                "/auth/login/transport",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = true,
                AccessTokenInHeader = false
            });

        mock.Setup(x => x.NavigateAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateFlowClient(mock);

        await client.LoginAsync(
            new LoginRequest
            {
                Identifier = "user",
                Secret = "pass"
            },
            returnUrl: "/home",
            transport: UAuthLoginTransport.Navigation);

        mock.Verify(x => x.NavigateAsync(
                "/auth/login",
                It.Is<IDictionary<string, string>>(d =>
                    d["Identifier"] == "user" &&
                    d["Secret"] == "pass" &&
                    d["__uauth_return_url"] == "/home"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Logout_Should_Observe_Server_Response()
    {
        var mock = new Mock<IUAuthRequestClient>();
        mock.Setup(x => x.SendFormAsync("/auth/logout", null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 200, Ok = true });
        var client = CreateFlowClient(mock);
        await client.LogoutAsync();
        mock.Verify(x => x.SendFormAsync("/auth/logout", null, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Logout_Should_Respect_Navigate_Option_And_Publish_Event(bool navigate)
    {
        var request = new Mock<IUAuthRequestClient>();
        var events = new Mock<IUAuthClientEvents>();

        request.Setup(x => x.SendFormAsync(
                "/auth/logout",
                null,
                navigate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Ok = true
            });

        var client = CreateFlowClient(request, events);

        await client.LogoutAsync(navigate: navigate);

        request.Verify(x => x.SendFormAsync(
            "/auth/logout",
            null,
            navigate,
            It.IsAny<CancellationToken>()), Times.Once);

        events.Verify(x => x.PublishAsync(
            It.Is<UAuthStateEventArgs>(e =>
                e.Type == UAuthStateEvent.LogoutVariant)),
            Times.Once);
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
            It.IsAny<IDictionary<string, string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(new UAuthTransportResult
        {
            Status = 200,
            Body = JsonSerializer.SerializeToElement(
                new AuthValidationResult
                {
                    State = SessionState.Active
                })
        });

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
            It.IsAny<IDictionary<string, string>?>(),
            It.IsAny<string?>(),
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
            new InMemoryUAuthTokenStore(),
            Mock.Of<IUAuthRefreshCoordinator>());

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

        await act.Should().ThrowAsync<UAuthProtocolException>().WithMessage("*authorization code*");
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
            new InMemoryUAuthTokenStore(),
            Mock.Of<IUAuthRefreshCoordinator>());

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
                    IsSuccess = true
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

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task TryCompletePkce_Should_Try_And_Navigate_When_Supported()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(
                It.IsAny<string>(),
                It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Status = 200,
                Ok = true,
                Body = JsonSerializer.SerializeToElement(
                    new TryPkceLoginResult
                    {
                        IsSuccess = true
                    })
            });

        mock.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = true,
                AccessTokenInHeader = false
            });

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
            UAuthSubmitMode.TryAndCommit);

        result.IsSuccess.Should().BeTrue();

        mock.Verify(x => x.SendJsonAsync(
            "/auth/pkce/try-complete",
            It.IsAny<object>()),
            Times.Once);

        mock.Verify(x => x.GetLoginTransportAsync(
            "/auth/login/transport",
            It.IsAny<CancellationToken>()),
            Times.Once);

        mock.Verify(x => x.NavigateAsync(
            "/auth/pkce/complete",
            It.Is<IDictionary<string, string>>(d =>
                d["authorization_code"] == "code" &&
                d["code_verifier"] == "verifier"),
            It.IsAny<CancellationToken>()),
            Times.Once);
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

        result.IsSuccess.Should().BeTrue();

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

    [Fact]
    public async Task CompletePkce_Should_Throw_When_Request_Null()
    {
        var client = CreateFlowClient();
        Func<Task> act = async () => await client.CompletePkceLoginAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task CompletePkce_Should_Throw_When_Pkce_Disabled()
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
            new InMemoryUAuthTokenStore(),
            Mock.Of<IUAuthRefreshCoordinator>());

        Func<Task> act = async () => await client.CompletePkceLoginAsync(new PkceCompleteRequest
        {
            AuthorizationCode = "code",
            CodeVerifier = "verifier",
            ReturnUrl = "/home",
            Identifier = "user",
            Secret = "pass"});

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LogoutUserDevice_Should_Call_Admin_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new RevokeResult())
            });

        var client = CreateFlowClient(mock);
        var userKey = UserKey.FromString("user-1");
        await client.LogoutUserDeviceAsync(userKey, new LogoutDeviceRequest() { ChainId = SessionChainId.New() });
        mock.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/logout-device", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task LogoutMyOtherDevices_Should_Call_Correct_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateFlowClient(mock);
        await client.LogoutMyOtherDevicesAsync();
        mock.Verify(x => x.SendJsonAsync("/auth/me/logout-others", null), Times.Once);
    }

    [Fact]
    public async Task LogoutUserOtherDevices_Should_Call_Admin_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateFlowClient(mock);
        var userKey = UserKey.FromString("user-1");
        await client.LogoutUserOtherDevicesAsync(userKey, new LogoutOtherDevicesRequest() { ExceptChainId = SessionChainId.New() });
        mock.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/logout-others", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAllUserDevices_Should_Call_Admin_Endpoint()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateFlowClient(mock);
        var userKey = UserKey.FromString("user-1");
        await client.LogoutAllUserDevicesAsync(userKey);
        mock.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/logout-all", null), Times.Once);
    }

    [Fact]
    public void UAuthClient_Should_Expose_FlowClient()
    {
        var flow = Mock.Of<IFlowClient>();

        var client = new UAuthClient(
            flow,
            Mock.Of<ISessionClient>(),
            Mock.Of<IUserClient>(),
            Mock.Of<IUserIdentifierClient>(),
            Mock.Of<ICredentialClient>(),
            Mock.Of<IAuthorizationClient>(),
            Mock.Of<IResourceClient>());

        client.Flows.Should().Be(flow);
    }

    [Theory]
    [InlineData("no-op", RefreshOutcome.NoOp)]
    [InlineData("touched", RefreshOutcome.Touched)]
    [InlineData("rotated", RefreshOutcome.Rotated)]
    [InlineData("reauth-required", RefreshOutcome.ReauthRequired)]
    public async Task Refresh_Should_Record_Correct_Diagnostic_Outcome(string outcomeHeader, RefreshOutcome expectedOutcome)
    {
        var request = new Mock<IUAuthRequestClient>();
        var diagnostics = new UAuthClientDiagnostics();

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = outcomeHeader
            });

        var client = CreateClient(request, diagnostics);

        var result = await client.RefreshAsync(isAuto: true);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(expectedOutcome);

        diagnostics.RefreshNoOpCount.Should()
            .Be(expectedOutcome == RefreshOutcome.NoOp ? 1 : 0);

        diagnostics.RefreshTouchedCount.Should()
            .Be(expectedOutcome == RefreshOutcome.Touched ? 1 : 0);

        diagnostics.RefreshRotatedCount.Should()
            .Be(expectedOutcome == RefreshOutcome.Rotated ? 1 : 0);

        diagnostics.RefreshReauthRequiredCount.Should()
            .Be(expectedOutcome == RefreshOutcome.ReauthRequired ? 1 : 0);

        diagnostics.RefreshSuccessCount.Should().Be(0);

        diagnostics.ManualRefreshCount.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_Should_Record_Success_Diagnostic()
    {
        var request = new Mock<IUAuthRequestClient>();
        var diagnostics = new UAuthClientDiagnostics();

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = "success"
            });

        var client = CreateClient(request, diagnostics);

        var result = await client.RefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Success);

        diagnostics.RefreshSuccessCount.Should().Be(1);
        diagnostics.ManualRefreshCount.Should().Be(1);
        diagnostics.RefreshAttemptCount.Should().Be(1);
    }

    [Theory]
    [InlineData("no-op")]
    [InlineData("touched")]
    [InlineData("rotated")]
    [InlineData("success")]
    public async Task Refresh_Should_Not_Record_Successful_Outcome_When_Response_Fails(string outcomeHeader)
    {
        var request = new Mock<IUAuthRequestClient>();
        var diagnostics = new UAuthClientDiagnostics();

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 200,
                RefreshOutcome = outcomeHeader
            });

        var client = CreateClient(request, diagnostics);

        var result = await client.RefreshAsync(isAuto: true);

        result.IsSuccess.Should().BeFalse();

        diagnostics.RefreshNoOpCount.Should().Be(0);
        diagnostics.RefreshTouchedCount.Should().Be(0);
        diagnostics.RefreshRotatedCount.Should().Be(0);
        diagnostics.RefreshSuccessCount.Should().Be(0);
    }

    [Fact]
    public async Task Refresh_Should_Record_ReauthRequired_On_Failed_Response()
    {
        var request = new Mock<IUAuthRequestClient>();
        var diagnostics = new UAuthClientDiagnostics();

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 400,
                RefreshOutcome = "reauth-required"
            });

        var client = CreateClient(request, diagnostics);

        var result = await client.RefreshAsync(isAuto: true);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(RefreshOutcome.ReauthRequired);

        diagnostics.RefreshReauthRequiredCount.Should().Be(1);

        diagnostics.RefreshSuccessCount.Should().Be(0);
        diagnostics.RefreshRotatedCount.Should().Be(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    public async Task Login_Auto_UsesFetch_WhenNavigationIsUnavailableOrNoReturnUrl(
        bool navigationSupported,
        bool accessTokenRequired)
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                "/auth/login/transport",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = navigationSupported,
                AccessTokenInHeader = accessTokenRequired
            });

        request.Setup(x => x.SendFormAsync(
                "/auth/login",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateClient(request);

        var result = await client.LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "secret" },
            returnUrl: navigationSupported ? null : "/home");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(200);

        request.Verify(x => x.SendFormAsync(
            "/auth/login",
            It.Is<IDictionary<string, string>>(data =>
                data["__uauth_response_mode"] == "fetch"),
            It.IsAny<CancellationToken>()), Times.Once);

        request.Verify(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_Auto_UsesNavigation_WhenSupportedAndReturnUrlExists()
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = true
            });

        request.Setup(x => x.NavigateAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await CreateClient(request).LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "secret" },
            returnUrl: "/home");

        result.IsSuccess.Should().BeTrue();
        result.Status.Should().Be(202);

        request.Verify(x => x.NavigateAsync(
            "/auth/login",
            It.Is<IDictionary<string, string>>(data =>
                data["__uauth_return_url"] == "/home"),
            It.IsAny<CancellationToken>()), Times.Once);

        request.Verify(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ExplicitNavigation_RejectsHeaderDelivery()
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = false,
                AccessTokenInHeader = true
            });

        var client = CreateClient(request);

        var act = () => client.LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "secret" },
            "/home",
            UAuthLoginTransport.Navigation);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*incompatible*");

        request.Verify(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_UnsupportedTransport_DoesNotSubmitCredentials()
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = false
            });

        var client = CreateClient(request);

        var act = () => client.LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "secret" });

        await act.Should().ThrowAsync<NotSupportedException>();

        request.Verify(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_Fetch_RejectsMissingRequiredAccessToken()
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = false,
                AccessTokenInHeader = true
            });

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateClient(request);

        var act = () => client.LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "secret" });

        await act.Should().ThrowAsync<UAuthProtocolException>()
            .WithMessage("*required access token*");
    }

    [Fact]
    public async Task Login_Fetch_RejectedResponse_DoesNotNavigate()
    {
        var request = new Mock<IUAuthRequestClient>();

        request.Setup(x => x.GetLoginTransportAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = true,
                NavigationSupported = false
            });

        request.Setup(x => x.SendFormAsync(
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 401
            });

        var result = await CreateClient(request).LoginAsync(
            new LoginRequest { Identifier = "alice", Secret = "wrong" },
            returnUrl: "/home");

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(401);

        request.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Logout_Success_ClearsTokenAndPublishesEvent(bool navigate)
    {
        var request = new Mock<IUAuthRequestClient>();
        var store = new InMemoryUAuthTokenStore();
        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        store.Replace(CreateStoredToken());

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        request.Setup(x => x.SendFormAsync(
                "/auth/logout",
                null,
                navigate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200
            });

        var client = CreateClient(
            request,
            tokenStore: store,
            clientEvents: events);

        await client.LogoutAsync(navigate);

        store.Read().Should().BeNull();
        published.Should().Equal(UAuthStateEvent.LogoutVariant);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(500)]
    public async Task Logout_Failure_PreservesTokenAndDoesNotPublishLogout(int status)
    {
        var request = new Mock<IUAuthRequestClient>();
        var store = new InMemoryUAuthTokenStore();
        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        var existing = CreateStoredToken();
        store.Replace(existing);

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        request.Setup(x => x.SendFormAsync(
                "/auth/logout",
                null,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = status
            });

        var client = CreateClient(
            request,
            tokenStore: store,
            clientEvents: events);

        var act = () => client.LogoutAsync(navigate: false);

        await act.Should().ThrowAsync<UAuthTransportException>();

        store.Read().Should().BeSameAs(existing);
        published.Should().BeEmpty();
    }

    [Fact]
    public async Task Refresh_Success_ReplacesExistingAccessToken()
    {
        var request = new Mock<IUAuthRequestClient>();
        var store = new InMemoryUAuthTokenStore();

        var existing = CreateStoredToken();
        store.Replace(existing);

        var expires = DateTimeOffset.UtcNow.AddMinutes(20);

        request.Setup(x => x.SendFormAsync(
                "/auth/refresh",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = "rotated",
                AccessToken = "Bearer rotated-jwt",
                AccessTokenFormat = "jwt",
                AccessTokenHeaderFormat = "bearer",
                AccessTokenExpiresAt = expires.ToString("O")
            });

        var client = CreateClient(request, tokenStore: store);

        var result = await client.RefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Rotated);

        var token = store.Read();

        token.Should().NotBeNull();
        token.Should().NotBeSameAs(existing);
        token!.Token.Should().Be("rotated-jwt");
        token.Format.Should().Be(TokenFormat.Jwt);
        token.ExpiresAt.Should().Be(expires);
    }

    [Fact]
    public async Task Refresh_NoOp_WithoutNewToken_PreservesExistingToken()
    {
        var request = new Mock<IUAuthRequestClient>();
        var store = new InMemoryUAuthTokenStore();

        var existing = CreateStoredToken();
        store.Replace(existing);

        request.Setup(x => x.SendFormAsync(
                "/auth/refresh",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                RefreshOutcome = "no-op"
            });

        var client = CreateClient(request, tokenStore: store);

        var result = await client.RefreshAsync();

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.NoOp);

        store.Read().Should().BeSameAs(existing);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Refresh_Unauthorized_OnlyRegularRefreshTriggersReauthentication(bool recovery)
    {
        var request = new Mock<IUAuthRequestClient>();
        var store = new InMemoryUAuthTokenStore();
        var diagnostics = new UAuthClientDiagnostics();
        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        var existing = CreateStoredToken();
        store.Replace(existing);

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        request.Setup(x => x.SendFormAsync(
                "/auth/refresh",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 401
            });

        var client = CreateClient(
            request,
            diagnostics,
            store,
            events);

        var result = recovery
            ? await client.RecoverAsync()
            : await client.RefreshAsync();

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(401);
        result.Outcome.Should().Be(RefreshOutcome.ReauthRequired);

        if (recovery)
        {
            store.Read().Should().BeSameAs(existing);
            published.Should().BeEmpty();
            diagnostics.RefreshReauthRequiredCount.Should().Be(0);
        }
        else
        {
            store.Read().Should().BeNull();

            published.Should()
                .Equal(UAuthStateEvent.ReauthenticationRequired);

            diagnostics.RefreshReauthRequiredCount.Should().Be(1);
        }
    }

    [Fact]
    public async Task CompletePkce_FetchHandoff_PreservesHubAuthenticationState()
    {
        const string returnUrl = "https://client.example.com/home";

        var mock = CreatePkceFetchRequestMock(
            CreatePkceFetchResponse(returnUrl));

        var store = new InMemoryUAuthTokenStore();
        var existing = CreateStoredToken();
        store.Replace(existing);

        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        var client = CreateClient(
            mock,
            tokenStore: store,
            clientEvents: events);

        await client.CompletePkceLoginAsync(new PkceCompleteRequest
        {
            Identifier = "user",
            Secret = "pass",
            AuthorizationCode = "code",
            CodeVerifier = "verifier",
            HubSessionId = "hub-session",
            ReturnUrl = returnUrl
        });

        mock.Verify(x => x.SendFormAsync(
            "/auth/pkce/complete",
            It.Is<IDictionary<string, string>>(d =>
                d["__uauth_response_mode"] == "fetch" &&
                d["authorization_code"] == "code" &&
                d["code_verifier"] == "verifier" &&
                d["hub_session_id"] == "hub-session"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        mock.Verify(x => x.NavigateToPkceReturnUrlAsync(
            returnUrl,
            returnUrl,
            It.IsAny<CancellationToken>()),
            Times.Once);

        mock.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        store.Read().Should().BeSameAs(existing);
        published.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null, "https://client.example.com/home")]
    [InlineData("", "https://client.example.com/home")]
    [InlineData(" ", "https://client.example.com/home")]
    [InlineData("https://client.example.com/home", null)]
    [InlineData("https://client.example.com/home", "")]
    [InlineData("https://client.example.com/home", " ")]
    public async Task CompletePkce_FetchHandoff_RejectsMissingRedirect(
    string? redirectUrl,
    string? returnUrl)
    {
        var mock = CreatePkceFetchRequestMock(
            CreatePkceFetchResponse(redirectUrl));

        var store = new InMemoryUAuthTokenStore();

        var client = CreateClient(mock, tokenStore: store);

        Func<Task> act = () => client.CompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                Identifier = "user",
                Secret = "pass",
                AuthorizationCode = "code",
                CodeVerifier = "verifier",
                HubSessionId = "hub-session",
                ReturnUrl = returnUrl
            });

        await act.Should()
            .ThrowAsync<UAuthProtocolException>()
            .WithMessage("Missing PKCE handoff redirect.");

        store.Read().Should().BeNull();

        mock.Verify(x => x.NavigateToPkceReturnUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        mock.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/home")]
    public async Task CompletePkce_FetchWithoutHandoff_StoresTokenAndPublishesLogin(
    string? redirectUrl)
    {
        var mock = CreatePkceFetchRequestMock(
            CreatePkceFetchResponse(redirectUrl));

        var store = new InMemoryUAuthTokenStore();
        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        var client = CreateClient(
            mock,
            tokenStore: store,
            clientEvents: events);

        await client.CompletePkceLoginAsync(new PkceCompleteRequest
        {
            Identifier = "user",
            Secret = "pass",
            AuthorizationCode = "code",
            CodeVerifier = "verifier",
            ReturnUrl = redirectUrl
        });

        var token = store.Read();

        token.Should().NotBeNull();
        token!.Token.Should().Be("pkce-jwt");
        token.Format.Should().Be(TokenFormat.Jwt);

        published.Should().Equal(UAuthStateEvent.LoginSucceeded);

        mock.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            redirectUrl is null ? Times.Never() : Times.Once());

        if (redirectUrl is not null)
        {
            mock.Verify(x => x.NavigateToUrlAsync(
                redirectUrl,
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        mock.Verify(x => x.NavigateToPkceReturnUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompletePkce_FetchFailure_PreservesExistingToken()
    {
        var mock = CreatePkceFetchRequestMock(
            new UAuthTransportResult
            {
                Ok = false,
                Status = 401
            });

        var store = new InMemoryUAuthTokenStore();
        var existing = CreateStoredToken();
        store.Replace(existing);

        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        var client = CreateClient(
            mock,
            tokenStore: store,
            clientEvents: events);

        Func<Task> act = () => client.CompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                Identifier = "user",
                Secret = "pass",
                AuthorizationCode = "code",
                CodeVerifier = "verifier"
            });

        await act.Should()
            .ThrowAsync<UAuthProtocolException>()
            .WithMessage("PKCE completion failed: HTTP 401.");

        store.Read().Should().BeSameAs(existing);
        published.Should().BeEmpty();

        mock.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        mock.Verify(x => x.NavigateToPkceReturnUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompletePkce_FetchWithoutRequiredToken_RejectsCompletion()
    {
        var mock = CreatePkceFetchRequestMock(
            new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = JsonSerializer.SerializeToElement(new
                {
                    redirectUrl = "/home"
                })
            });

        var store = new InMemoryUAuthTokenStore();

        var client = CreateClient(mock, tokenStore: store);

        Func<Task> act = () => client.CompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                Identifier = "user",
                Secret = "pass",
                AuthorizationCode = "code",
                CodeVerifier = "verifier"
            });

        await act.Should().ThrowAsync<UAuthProtocolException>();

        store.Read().Should().BeNull();

        mock.Verify(x => x.NavigateToUrlAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CompletePkce_UnsupportedTransport_DoesNotSubmitCredentials()
    {
        var mock = new Mock<IUAuthRequestClient>();

        mock.Setup(x => x.GetLoginTransportAsync(
                "/auth/login/transport",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthLoginTransportInfo
            {
                Supported = false,
                NavigationSupported = false,
                AccessTokenInHeader = false
            });

        var client = CreateClient(mock);

        Func<Task> act = () => client.CompletePkceLoginAsync(
            new PkceCompleteRequest
            {
                Identifier = "user",
                Secret = "pass",
                AuthorizationCode = "code",
                CodeVerifier = "verifier"
            });

        await act.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("PKCE credential delivery is not supported.");

        mock.Verify(x => x.SendFormAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        mock.Verify(x => x.NavigateAsync(
            It.IsAny<string>(),
            It.IsAny<IDictionary<string, string>>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

public abstract class UAuthClientTestBase
{
    protected readonly Mock<IUAuthRequestClient> Request = new();
    protected readonly Mock<IUAuthClientEvents> Events = new();

    protected IUAuthClient CreateClient(
        IUserClient? users = null,
        ISessionClient? sessions = null,
        ICredentialClient? credentials = null,
        IAuthorizationClient? authorization = null,
        IFlowClient? flows = null,
        IUserIdentifierClient? identifiers = null)
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

        return new UAuthClient(
            flows ?? Mock.Of<IFlowClient>(),
            sessions ?? new UAuthSessionClient(Request.Object, options, Events.Object),
            users ?? new UAuthUserClient(Request.Object, Events.Object, options),
            identifiers ?? Mock.Of<IUserIdentifierClient>(),
            credentials ?? new UAuthCredentialClient(Request.Object, Events.Object, options),
            authorization ?? new UAuthAuthorizationClient(Request.Object, Events.Object, options)
        );
    }

    protected IFlowClient CreateFlowClient(
        Mock<IUAuthRequestClient>? requestMock = null,
        Mock<IUAuthClientEvents>? eventsMock = null)
    {
        var request = requestMock ?? new Mock<IUAuthRequestClient>();
        var events = eventsMock ?? new Mock<IUAuthClientEvents>();

        var deviceProvider = new Mock<IClientDeviceProvider>();
        deviceProvider.Setup(x => x.GetAsync())
            .ReturnsAsync(DeviceContext.Create(DeviceId.Create("device-123456789123456789123456789123456789"), "web"));

        var returnUrlProvider = new Mock<IReturnUrlProvider>();
        returnUrlProvider.Setup(x => x.GetCurrentUrl())
            .Returns("/home");

        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth",
                Login = "/login",
                TryLogin = "/try-login",
                Logout = "/logout",
                Refresh = "/refresh",
                Validate = "/validate"
            },
            Login = new UAuthClientLoginFlowOptions
            {
                AllowCredentialPost = true
            },
            Pkce = new UAuthClientPkceLoginFlowOptions
            {
                Enabled = true
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

    protected IUAuthClient CreateCredentialClient()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: Mock.Of<ISessionClient>(),
            users: Mock.Of<IUserClient>(),
            identifiers: Mock.Of<IUserIdentifierClient>(),
            credentials: new UAuthCredentialClient(Request.Object, Events.Object, options),
            authorization: Mock.Of<IAuthorizationClient>());
    }

    protected IUAuthClient CreateUserClient()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: Mock.Of<ISessionClient>(),
            users: new UAuthUserClient(Request.Object, Events.Object, options),
            identifiers: Mock.Of<IUserIdentifierClient>(),
            credentials: Mock.Of<ICredentialClient>(),
            authorization: Mock.Of<IAuthorizationClient>());
    }

    protected IUAuthClient CreateIdentifierClient()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: Mock.Of<ISessionClient>(),
            users: Mock.Of<IUserClient>(),
            identifiers: new UAuthUserIdentifierClient(Request.Object, Events.Object, options),
            credentials: Mock.Of<ICredentialClient>(),
            authorization: Mock.Of<IAuthorizationClient>());
    }

    protected static UAuthTransportResult Success()
        => new() { Ok = true, Status = 200 };

    protected static UAuthTransportResult Failure(int status = 400)
        => new() { Ok = false, Status = status };

    protected static UAuthTransportResult SuccessJson<T>(T body)
        => new()
        {
            Ok = true,
            Status = 200,
            Body = JsonSerializer.SerializeToElement(body)
        };
}
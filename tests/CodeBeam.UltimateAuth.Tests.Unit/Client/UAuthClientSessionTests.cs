using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientSessionTests
{
    private readonly Mock<IUAuthRequestClient> _request = new();
    private readonly Mock<IUAuthClientEvents> _events = new();

    private IUAuthClient CreateClient()
    {
        var options = Options.Create(new UAuthClientOptions
        {
            Endpoints = new UAuthClientEndpointOptions
            {
                BasePath = "/auth"
            }
        });

        var sessionClient = new UAuthSessionClient(_request.Object, options, _events.Object);

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: sessionClient,
            users: Mock.Of<IUserClient>(),
            identifiers: Mock.Of<IUserIdentifierClient>(),
            credentials: Mock.Of<ICredentialClient>(),
            authorization: Mock.Of<IAuthorizationClient>());
    }

    private static UAuthTransportResult Success()
    => new() { Ok = true, Status = 200 };

    private static UAuthTransportResult SuccessJson<T>(T body)
        => new()
        {
            Ok = true,
            Status = 200,
            Body = JsonSerializer.SerializeToElement(body)
        };

    [Fact]
    public async Task GetMyChains_Should_Call_Correct_Endpoint()
    {
        var response = new PagedResult<SessionChainSummary>(
            new List<SessionChainSummary>(),
            0, 1, 10, null, false);

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        await client.Sessions.GetMyChainsAsync();
        _request.Verify(x => x.SendJsonAsync("/auth/me/sessions/chains", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetMyChainDetail_Should_Call_Correct_Endpoint()
    {
        var chainId = SessionChainId.New();

        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(new SessionChainDetail
            {
                ChainId = chainId
            }));

        var client = CreateClient();
        await client.Sessions.GetMyChainDetailAsync(chainId);
        _request.Verify(x => x.SendFormAsync($"/auth/me/sessions/chains/{chainId.Value}"), Times.Once);
    }

    [Fact]
    public async Task GetUserChainDetail_Should_Call_Correct_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");
        var chainId = SessionChainId.New();

        var response = new SessionChainDetail
        {
            ChainId = chainId
        };

        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        await client.Sessions.GetUserChainDetailAsync(userKey, chainId);
        _request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/sessions/chains/{chainId.Value}"), Times.Once);
    }

    [Fact]
    public async Task RevokeMyChain_Should_Publish_Event_When_CurrentChain()
    {
        var response = new RevokeResult
        {
            CurrentChain = true
        };

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        var chainId = SessionChainId.From(Guid.NewGuid());

        await client.Sessions.RevokeMyChainAsync(chainId);

        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.SessionRevoked)), Times.Once);
    }

    [Fact]
    public async Task RevokeMyChain_Should_NOT_Publish_Event_When_Not_CurrentChain()
    {
        var response = new RevokeResult
        {
            CurrentChain = false
        };

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        var chainId = SessionChainId.From(Guid.NewGuid());
        await client.Sessions.RevokeMyChainAsync(chainId);

        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task RevokeUserChain_Should_Call_Correct_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");
        var chainId = SessionChainId.New();

        var response = new RevokeResult
        {
            CurrentChain = false
        };

        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        await client.Sessions.RevokeUserChainAsync(userKey, chainId);
        _request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/sessions/chains/{chainId.Value}/revoke"), Times.Once);
    }

    [Fact]
    public async Task RevokeAllMyChains_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();
        await client.Sessions.RevokeAllMyChainsAsync();
        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.SessionRevoked)), Times.Once);
    }

    [Fact]
    public async Task RevokeAllMyChains_Should_NOT_Publish_Event_On_Failure()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateClient();
        await client.Sessions.RevokeAllMyChainsAsync();
        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task RevokeAllUserChains_Should_Call_Correct_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(Success());

        var client = CreateClient();
        await client.Sessions.RevokeAllUserChainsAsync(userKey);
        _request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/sessions/revoke-all"), Times.Once);
    }

    [Fact]
    public async Task RevokeMyOtherChains_Should_Call_Correct_Endpoint()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(Success);

        var client = CreateClient();
        await client.Sessions.RevokeMyOtherChainsAsync();
        _request.Verify(x => x.SendFormAsync("/auth/me/sessions/revoke-others"), Times.Once);
    }

    [Fact]
    public async Task GetUserChains_Should_Call_Admin_Endpoint()
    {
        var response = new PagedResult<SessionChainSummary>(
            new List<SessionChainSummary>(),
            0, 1, 10, null, false);

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");
        await client.Sessions.GetUserChainsAsync(userKey);

        _request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/sessions/chains", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RevokeUserSession_Should_Call_Correct_Endpoint()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(Success);

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");
        var sessionId = AuthSessionId.Parse("session-123456789123456789123456789", null);

        await client.Sessions.RevokeUserSessionAsync(userKey, sessionId);

        _request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/sessions/{sessionId.Value}/revoke"), Times.Once);
    }

    [Fact]
    public async Task RevokeUserRoot_Should_Call_Correct_Endpoint()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(Success);

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");
        await client.Sessions.RevokeUserRootAsync(userKey);
        _request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/sessions/revoke-root"), Times.Once);
    }

    [Fact]
    public async Task RevokeMyChain_Should_NOT_Publish_Event_When_Value_Null()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 200,
                Body = null
            });

        var client = CreateClient();
        var chainId = SessionChainId.New();
        await client.Sessions.RevokeMyChainAsync(chainId);
        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }
}

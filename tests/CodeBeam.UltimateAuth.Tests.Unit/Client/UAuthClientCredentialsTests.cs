using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientCredentialTests
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

        var credentialClient = new UAuthCredentialClient(
            _request.Object,
            _events.Object,
            options);

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: Mock.Of<ISessionClient>(),
            users: Mock.Of<IUserClient>(),
            identifiers: Mock.Of<IUserIdentifierClient>(),
            credentials: credentialClient,
            authorization: Mock.Of<IAuthorizationClient>());
    }

    private static UAuthTransportResult SuccessJson<T>(T body)
    {
        return new UAuthTransportResult
        {
            Ok = true,
            Status = 200,
            Body = JsonSerializer.SerializeToElement(body)
        };
    }

    [Fact]
    public async Task AddMy_Should_Call_Correct_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new AddCredentialResult()));

        var client = CreateClient();

        await client.Credentials.AddMyAsync(new AddCredentialRequest() { Secret = "uauth" });

        _request.Verify(x =>
            x.SendJsonAsync("/auth/me/credentials/add", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task ChangeMy_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new ChangeCredentialResult()));

        var client = CreateClient();

        await client.Credentials.ChangeMyAsync(new ChangeCredentialRequest() { NewSecret = "uauth" });

        _events.Verify(x =>
            x.PublishAsync(It.Is<UAuthStateEventArgsEmpty>(e =>
                e.Type == UAuthStateEvent.CredentialsChangedSelf)),
            Times.Once);
    }

    [Fact]
    public async Task ChangeMy_Should_NOT_Publish_Event_On_Failure()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateClient();

        await client.Credentials.ChangeMyAsync(new ChangeCredentialRequest() { NewSecret = "uauth" });

        _events.Verify(x =>
            x.PublishAsync(It.IsAny<UAuthStateEventArgs>()),
            Times.Never);
    }

    [Fact]
    public async Task RevokeMy_Should_Publish_Event()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();

        await client.Credentials.RevokeMyAsync(new RevokeCredentialRequest());

        _events.Verify(x =>
            x.PublishAsync(It.IsAny<UAuthStateEventArgsEmpty>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUser_Should_Call_Admin_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new AddCredentialResult()));

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.AddUserAsync(userKey, new AddCredentialRequest() { Secret = "uauth" });

        _request.Verify(x =>
            x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/add", It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Call_Delete_Endpoint()
    {
        _request.Setup(x => x.SendFormAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.DeleteUserAsync(userKey, new DeleteCredentialRequest());

        _request.Verify(x =>
            x.SendFormAsync($"/auth/admin/users/{userKey.Value}/credentials/delete",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginResetMy_Should_Call_Correct_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new BeginCredentialResetResult()));

        var client = CreateClient();

        await client.Credentials.BeginResetMyAsync(new BeginResetCredentialRequest() { Identifier = "user1" });

        _request.Verify(x => x.SendJsonAsync("/auth/me/credentials/reset/begin", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CompleteResetMy_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new CredentialActionResult()));

        var client = CreateClient();

        await client.Credentials.CompleteResetMyAsync(new CompleteResetCredentialRequest() { NewSecret = "uauth" });

        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.CredentialsChanged)), Times.Once);
    }

    [Fact]
    public async Task CompleteResetMy_Should_NOT_Publish_Event_On_Failure()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateClient();

        await client.Credentials.CompleteResetMyAsync(new CompleteResetCredentialRequest() { NewSecret = "uauth" });

        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task BeginResetUser_Should_Call_Admin_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new BeginCredentialResetResult()));

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.BeginResetUserAsync(userKey, new BeginResetCredentialRequest() { Identifier = "user1" });

        _request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/reset/begin", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CompleteResetUser_Should_NOT_Publish_Event()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new CredentialActionResult()));

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.CompleteResetUserAsync(userKey, new CompleteResetCredentialRequest() { NewSecret = "uauth" });

        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUser_Should_Call_Admin_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new ChangeCredentialResult()));

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.ChangeUserAsync(userKey, new ChangeCredentialRequest() { NewSecret = "uauth" });

        _request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/change", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RevokeUser_Should_Call_Admin_Endpoint()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.RevokeUserAsync(userKey, new RevokeCredentialRequest());

        _request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/revoke", It.IsAny<object>()), Times.Once);
    }
}

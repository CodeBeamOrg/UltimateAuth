using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientCredentialTests : UAuthClientTestBase
{
    [Fact]
    public async Task AddMy_Should_Call_Correct_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new AddCredentialResult()));

        var client = CreateCredentialClient();
        await client.Credentials.AddMyAsync(new AddCredentialRequest() { Secret = "uauth" });
        Request.Verify(x => x.SendJsonAsync("/auth/me/credentials/add", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ChangeMy_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new ChangeCredentialResult()));

        var client = CreateCredentialClient();
        await client.Credentials.ChangeMyAsync(new ChangeCredentialRequest() { NewSecret = "uauth" });
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgsEmpty>(e => e.Type == UAuthStateEvent.CredentialsChangedSelf)), Times.Once);
    }

    [Fact]
    public async Task ChangeMy_Should_NOT_Publish_Event_On_Failure()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateCredentialClient();
        await client.Credentials.ChangeMyAsync(new ChangeCredentialRequest() { NewSecret = "uauth" });
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task RevokeMy_Should_Publish_Event()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateCredentialClient();
        await client.Credentials.RevokeMyAsync(new RevokeCredentialRequest());
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgsEmpty>()), Times.Once);
    }

    [Fact]
    public async Task AddUser_Should_Call_Admin_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new AddCredentialResult()));

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");
        await client.Credentials.AddUserAsync(userKey, new AddCredentialRequest() { Secret = "uauth" });
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/add", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Call_Delete_Endpoint()
    {
        Request.Setup(x => x.SendFormAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");
        await client.Credentials.DeleteUserAsync(userKey, new DeleteCredentialRequest());
        Request.Verify(x =>
            x.SendFormAsync($"/auth/admin/users/{userKey.Value}/credentials/delete",
                It.IsAny<IDictionary<string, string>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task BeginResetMy_Should_Call_Correct_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new BeginCredentialResetResult()));

        var client = CreateCredentialClient();
        await client.Credentials.BeginResetMyAsync(new BeginResetCredentialRequest() { Identifier = "user1" });
        Request.Verify(x => x.SendJsonAsync("/auth/me/credentials/reset/begin", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CompleteResetMy_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new CredentialActionResult()));

        var client = CreateCredentialClient();
        await client.Credentials.CompleteResetMyAsync(new CompleteResetCredentialRequest() { NewSecret = "uauth" });
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.CredentialsChanged)), Times.Once);
    }

    [Fact]
    public async Task CompleteResetMy_Should_NOT_Publish_Event_On_Failure()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateCredentialClient();
        await client.Credentials.CompleteResetMyAsync(new CompleteResetCredentialRequest() { NewSecret = "uauth" });
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task BeginResetUser_Should_Call_Admin_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new BeginCredentialResetResult()));

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.BeginResetUserAsync(userKey, new BeginResetCredentialRequest() { Identifier = "user1" });

        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/reset/begin", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CompleteResetUser_Should_NOT_Publish_Event()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new CredentialActionResult()));

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");
        await client.Credentials.CompleteResetUserAsync(userKey, new CompleteResetCredentialRequest() { NewSecret = "uauth" });
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task ChangeUser_Should_Call_Admin_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new ChangeCredentialResult()));

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");

        await client.Credentials.ChangeUserAsync(userKey, new ChangeCredentialRequest() { NewSecret = "uauth" });

        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/change", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task RevokeUser_Should_Call_Admin_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateCredentialClient();
        var userKey = UserKey.FromString("user-1");
        await client.Credentials.RevokeUserAsync(userKey, new RevokeCredentialRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/credentials/revoke", It.IsAny<object>()), Times.Once);
    }
}

using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientUserIdentifiersTests : UAuthClientTestBase
{
    [Fact]
    public async Task GetMy_Should_Call_Correct_Endpoint()
    {
        var response = new PagedResult<UserIdentifierInfo>(
            new List<UserIdentifierInfo>(),
            0, 1, 10, null, false);

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateIdentifierClient();
        await client.Identifiers.GetMyAsync();
        Request.Verify(x => x.SendJsonAsync("/auth/me/identifiers/get", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task AddMy_Should_Publish_Event_On_Success()
    {
        var request = new AddUserIdentifierRequest() { Value = "uauth" };

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.AddMyAsync(request);

        Events.Verify(x =>
            x.PublishAsync(It.Is<UAuthStateEventArgs<AddUserIdentifierRequest>>(e =>
                e.Type == UAuthStateEvent.IdentifiersChanged &&
                e.Payload == request)),
            Times.Once);
    }

    [Fact]
    public async Task AddMy_Should_NOT_Publish_Event_On_Failure()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Failure());

        var client = CreateIdentifierClient();
        await client.Identifiers.AddMyAsync(new AddUserIdentifierRequest() { Value = "uauth" });
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMy_Should_Publish_Event_On_Success()
    {
        var request = new UpdateUserIdentifierRequest() { NewValue = "uauth" };

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.UpdateMyAsync(request);
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs<UpdateUserIdentifierRequest>>(e => e.Payload == request)), Times.Once);
    }

    [Fact]
    public async Task SetMyPrimary_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.SetMyPrimaryAsync(new SetPrimaryUserIdentifierRequest());
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.IdentifiersChanged)), Times.Once);
    }

    [Fact]
    public async Task UnsetMyPrimary_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.UnsetMyPrimaryAsync(new UnsetPrimaryUserIdentifierRequest());
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.IdentifiersChanged)), Times.Once);
    }

    [Fact]
    public async Task VerifyMy_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.VerifyMyAsync(new VerifyUserIdentifierRequest());
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.IdentifiersChanged)), Times.Once);
    }

    [Fact]
    public async Task DeleteMy_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.DeleteMyAsync(new DeleteUserIdentifierRequest());
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.IdentifiersChanged)), Times.Once);
    }

    [Fact]
    public async Task GetUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        var response = new PagedResult<UserIdentifierInfo>(
            new List<UserIdentifierInfo>(),
            0, 1, 10, null, false);

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateIdentifierClient();
        await client.Identifiers.GetUserAsync(userKey);
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/identifiers/get", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task AddUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.AddUserAsync(userKey, new AddUserIdentifierRequest() { Value = "uauth"});
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey}/identifiers/add", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.DeleteUserAsync(userKey, new DeleteUserIdentifierRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey}/identifiers/delete", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.UpdateUserAsync(userKey, new UpdateUserIdentifierRequest() { NewValue = "uauth" });
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/identifiers/update", It.IsAny<object>()), Times.Once);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task SetUserPrimary_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.SetUserPrimaryAsync(userKey, new SetPrimaryUserIdentifierRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/identifiers/set-primary", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task UnsetUserPrimary_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.UnsetUserPrimaryAsync(userKey, new UnsetPrimaryUserIdentifierRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/identifiers/unset-primary", It.IsAny<object>()), Times.Once);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task VerifyUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateIdentifierClient();
        await client.Identifiers.VerifyUserAsync(userKey, new VerifyUserIdentifierRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/identifiers/verify", It.IsAny<object>()), Times.Once);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }
}

using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.Contracts;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientUserTests : UAuthClientTestBase
{
    [Fact]
    public async Task GetMe_Should_Call_Correct_Endpoint()
    {
        var response = new UserView
        {
            UserKey = UserKey.FromString("user-1")
        };

        Request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateUserClient();
        await client.Users.GetMeAsync();
        Request.Verify(x => x.SendFormAsync("/auth/me/get"), Times.Once);
    }

    [Fact]
    public async Task UpdateMe_Should_Publish_Event_On_Success()
    {
        var request = new UpdateProfileRequest();

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateUserClient();
        await client.Users.UpdateMeAsync(request);
        Events.Verify(x =>
            x.PublishAsync(It.Is<UAuthStateEventArgs<UpdateProfileRequest>>(e =>
                e.Type == UAuthStateEvent.ProfileChanged &&
                e.Payload == request)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateMe_Should_NOT_Publish_Event_On_Failure()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateUserClient();
        await client.Users.UpdateMeAsync(new UpdateProfileRequest());
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task DeleteMe_Should_Publish_Event_On_Success()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>()))
            .ReturnsAsync(Success());

        var client = CreateUserClient();
        await client.Users.DeleteMeAsync();
        Events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.UserDeleted)), Times.Once);
    }

    [Fact]
    public async Task Query_Should_Call_Admin_Endpoint()
    {
        var response = new PagedResult<UserSummary>(
            new List<UserSummary>(),
            0, 1, 10, null, false);

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateUserClient();
        await client.Users.QueryAsync(new UserQuery());
        Request.Verify(x => x.SendJsonAsync("/auth/admin/users/query", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Create_Should_Call_Public_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new UserCreateResult() { Succeeded = true }));

        var client = CreateUserClient();
        await client.Users.CreateAsync(new CreateUserRequest());
        Request.Verify(x => x.SendJsonAsync("/auth/users/create", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsAdmin_Should_Call_Admin_Endpoint()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new UserCreateResult() { Succeeded = true}));

        var client = CreateUserClient();
        await client.Users.CreateAsAdminAsync(new CreateUserRequest());
        Request.Verify(x => x.SendJsonAsync("/auth/admin/users/create", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task ChangeMyStatus_Should_Publish_Event_On_Success()
    {
        var request = new ChangeUserStatusSelfRequest() { NewStatus = SelfAssignableUserStatus.Active };

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new UserStatusChangeResult() { Succeeded = true }));

        var client = CreateUserClient();
        await client.Users.ChangeMyStatusAsync(request);
        Events.Verify(x =>
            x.PublishAsync(It.Is<UAuthStateEventArgs<ChangeUserStatusSelfRequest>>(e =>
                e.Type == UAuthStateEvent.ProfileChanged &&
                e.Payload == request)),
            Times.Once);
    }

    [Fact]
    public async Task ChangeUserStatus_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new UserStatusChangeResult() { Succeeded = true }));

        var client = CreateUserClient();
        await client.Users.ChangeUserStatusAsync(userKey, new ChangeUserStatusAdminRequest() { NewStatus = AdminAssignableUserStatus.Active });
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/status", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new UserDeleteResult() { Succeeded = true, Mode = DeleteMode.Soft }));

        var client = CreateUserClient();
        await client.Users.DeleteUserAsync(userKey, new DeleteUserRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/delete", It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task GetUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        var response = new UserView
        {
            UserKey = userKey
        };

        Request.Setup(x => x.SendFormAsync(It.IsAny<string>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateUserClient();
        await client.Users.GetUserAsync(userKey);
        Request.Verify(x => x.SendFormAsync($"/auth/admin/users/{userKey.Value}/profile/get"), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateUserClient();
        await client.Users.UpdateUserAsync(userKey, new UpdateProfileRequest());
        Request.Verify(x => x.SendJsonAsync($"/auth/admin/users/{userKey.Value}/profile/update", It.IsAny<object>()), Times.Once);
    }
}

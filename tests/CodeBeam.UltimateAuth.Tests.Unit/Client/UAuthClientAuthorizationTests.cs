using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientAuthorizationTests
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

        var authorizationClient = new UAuthAuthorizationClient(
            _request.Object,
            _events.Object,
            options);

        return new UAuthClient(
            flows: Mock.Of<IFlowClient>(),
            session: Mock.Of<ISessionClient>(),
            users: Mock.Of<IUserClient>(),
            identifiers: Mock.Of<IUserIdentifierClient>(),
            credentials: Mock.Of<ICredentialClient>(),
            authorization: authorizationClient);
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
    public async Task AssignRole_Should_Call_Correct_Endpoint_And_Publish_Event()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateClient();

        var request = new AssignRoleRequest
        {
            UserKey = UserKey.FromString("user-1"),
            RoleName = "admin"
        };

        await client.Authorization.AssignRoleToUserAsync(request);

        _request.Verify(x => x.SendJsonAsync( $"/auth/admin/authorization/users/{request.UserKey.Value}/roles/assign", request.RoleName), Times.Once);
        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.AuthorizationChanged)), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(Success());

        var client = CreateClient();

        var request = new RemoveRoleRequest
        {
            UserKey = UserKey.FromString("user-1"),
            RoleName = "admin"
        };

        await client.Authorization.RemoveRoleFromUserAsync(request);

        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.AuthorizationChanged)), Times.Once);
    }

    [Fact]
    public async Task AssignRole_Should_NOT_Publish_Event_On_Failure()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateClient();

        var request = new AssignRoleRequest
        {
            UserKey = UserKey.FromString("user-1"),
            RoleName = "admin"
        };

        await client.Authorization.AssignRoleToUserAsync(request);

        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task Check_Should_Return_Result()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new AuthorizationResult
            {
                IsAllowed = true
            }));

        var client = CreateClient();

        var result = await client.Authorization.CheckAsync(new AuthorizationCheckRequest() { Action = UAuthActions.Authorization.Roles.CreateAdmin });

        result.IsSuccess.Should().BeTrue();
        result.Value!.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task QueryRoles_Should_Return_Data()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new PagedResult<RoleInfo>(
                new List<RoleInfo>(),
                0, 1, 10, null, false)));

        var client = CreateClient();

        var result = await client.Authorization.QueryRolesAsync(new RoleQuery());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyRoles_Should_Call_Correct_Endpoint_And_Return_Data()
    {
        var userKey = UserKey.FromString("user-1");

        var response = new UserRolesResponse
        {
            UserKey = userKey,
            Roles = new PagedResult<UserRoleInfo>(
                new List<UserRoleInfo>
                {
                new UserRoleInfo
                {
                    Tenant = TenantKeys.Single,
                    UserKey = userKey,
                    RoleId = RoleId.From(Guid.NewGuid()),
                    Name = "admin",
                    AssignedAt = DateTimeOffset.UtcNow
                }
                },
                totalCount: 1,
                pageNumber: 1,
                pageSize: 10,
                sortBy: null,
                descending: false)
        };

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();

        var result = await client.Authorization.GetMyRolesAsync();

        _request.Verify(x =>
            x.SendJsonAsync("/auth/me/authorization/roles/get", It.IsAny<object>()),
            Times.Once);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserKey.Should().Be(userKey);
        result.Value.Roles.Items.Should().HaveCount(1);
        result.Value.Roles.Items[0].Name.Should().Be("admin");
    }

    [Fact]
    public async Task GetUserRoles_Should_Call_Admin_Endpoint()
    {
        var userKey = UserKey.FromString("user-1");

        var response = new UserRolesResponse
        {
            UserKey = userKey,
            Roles = new PagedResult<UserRoleInfo>(
                new List<UserRoleInfo>
                {
                new UserRoleInfo
                {
                    Tenant = TenantKeys.Single,
                    UserKey = userKey,
                    RoleId = RoleId.From(Guid.NewGuid()),
                    Name = "admin",
                    AssignedAt = DateTimeOffset.UtcNow
                }
                },
                totalCount: 1,
                pageNumber: 1,
                pageSize: 10,
                sortBy: null,
                descending: false)
        };

        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(response));

        var client = CreateClient();
        var result = await client.Authorization.GetUserRolesAsync(userKey);
        _request.Verify(x => x.SendJsonAsync($"/auth/admin/authorization/users/{userKey.Value}/roles/get", It.IsAny<object>()), Times.Once);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.UserKey.Should().Be(userKey);
        result.Value.Roles.Items.Should().HaveCount(1);
        result.Value.Roles.Items[0].Name.Should().Be("admin");
    }

    [Fact]
    public async Task CreateRole_Should_Return_Result()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new RoleInfo() { Name = "admin" }));

        var client = CreateClient();

        var result = await client.Authorization.CreateRoleAsync(new CreateRoleRequest() { Name = "admin" });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task RenameRole_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();

        var request = new RenameRoleRequest
        {
            Id = RoleId.From(Guid.NewGuid()),
            Name = "new-role"
        };

        await client.Authorization.RenameRoleAsync(request);

        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.AuthorizationChanged)), Times.Once);
    }

    [Fact]
    public async Task RenameRole_Should_NOT_Publish_Event_On_Failure()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = false, Status = 400 });

        var client = CreateClient();

        await client.Authorization.RenameRoleAsync(new RenameRoleRequest
        {
            Id = RoleId.From(Guid.NewGuid()),
            Name = "fail"
        });

        _events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()), Times.Never);
    }

    [Fact]
    public async Task SetRolePermissions_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        var client = CreateClient();

        var request = new SetRolePermissionsRequest
        {
            RoleId = RoleId.From(Guid.NewGuid()),
            Permissions = new List<Permission> { Permission.From("read"), Permission.From("write") }
        };

        await client.Authorization.SetRolePermissionsAsync(request);

        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.AuthorizationChanged)), Times.Once);
    }

    [Fact]
    public async Task DeleteRole_Should_Publish_Event_On_Success()
    {
        _request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>()))
            .ReturnsAsync(SuccessJson(new DeleteRoleResult()));

        var client = CreateClient();

        var request = new DeleteRoleRequest
        {
            Id = RoleId.From(Guid.NewGuid())
        };

        await client.Authorization.DeleteRoleAsync(request);
        _events.Verify(x => x.PublishAsync(It.Is<UAuthStateEventArgs>(e => e.Type == UAuthStateEvent.AuthorizationChanged)), Times.Once);
    }
}

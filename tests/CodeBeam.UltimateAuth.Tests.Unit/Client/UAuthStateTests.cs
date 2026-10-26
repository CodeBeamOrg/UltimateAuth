using System.Security.Claims;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthStateTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    private static AuthStateSnapshot Snapshot(
        string userKey = "user-1",
        string displayName = "Original Name",
        params string[] permissions)
    {
        var claims = ClaimsSnapshot.Create()
            .AddRole("Member")
            .Add("custom:department", "engineering")
            .AddMany("uauth:permission", permissions)
            .Build();

        return new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = UserKey.FromString(userKey),
                Tenant = TenantKey.Single,
                PrimaryUserName = userKey,
                DisplayName = displayName
            },
            Claims = claims
        };
    }

    private static UAuthState Authenticated(
        params string[] permissions)
    {
        var state = UAuthState.Anonymous();
        state.ApplySnapshot(Snapshot(permissions: permissions), Now);
        return state;
    }

    [Fact]
    public void Anonymous_Should_Have_Empty_State()
    {
        var state = UAuthState.Anonymous();

        state.IsAuthenticated.Should().BeFalse();
        state.NeedsValidation.Should().BeFalse();
        state.IsStale.Should().BeFalse();
        state.Identity.Should().BeNull();
        state.LastValidatedAt.Should().BeNull();

        state.HasPermission("users.read").Should().BeFalse();
        state.HasAnyPermission("users.read", "users.write").Should().BeFalse();
        state.IsInRole("Member").Should().BeFalse();
        state.HasClaim("custom:department", "engineering").Should().BeFalse();
        state.GetClaim("custom:department").Should().BeNull();
    }

    [Fact]
    public void ApplySnapshot_Should_Set_Identity_Claims_And_Validation_Time()
    {
        var state = UAuthState.Anonymous();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.ApplySnapshot(
            Snapshot(permissions: ["users.read"]),
            Now);

        state.IsAuthenticated.Should().BeTrue();
        state.IsStale.Should().BeFalse();
        state.NeedsValidation.Should().BeFalse();
        state.LastValidatedAt.Should().Be(Now);

        state.Identity!.DisplayName.Should().Be("Original Name");
        state.HasPermission("users.read").Should().BeTrue();
        state.IsInRole("Member").Should().BeTrue();
        state.HasClaim("custom:department", "engineering").Should().BeTrue();
        state.GetClaim("custom:department").Should().Be("engineering");

        reasons.Should().ContainSingle()
            .Which.Should().Be(UAuthStateChangeReason.Authenticated);
    }

    [Fact]
    public void UpdateProfile_Should_Update_DisplayName_And_Raise_Patched()
    {
        var state = Authenticated();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.UpdateProfile(new UpdateProfileRequest
        {
            DisplayName = "Updated Name"
        });

        state.Identity!.DisplayName.Should().Be("Updated Name");
        state.Identity.UserKey.Should().Be(UserKey.FromString("user-1"));

        reasons.Should().ContainSingle()
            .Which.Should().Be(UAuthStateChangeReason.Patched);
    }

    [Fact]
    public void UpdateProfile_With_Null_DisplayName_Should_Preserve_Name()
    {
        var state = Authenticated();

        state.UpdateProfile(new UpdateProfileRequest
        {
            FirstName = "Updated"
        });

        state.Identity!.DisplayName.Should().Be("Original Name");
    }

    [Fact]
    public void UpdateProfile_Should_Do_Nothing_When_Anonymous()
    {
        var state = UAuthState.Anonymous();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.UpdateProfile(new UpdateProfileRequest
        {
            DisplayName = "New Name"
        });

        state.Identity.Should().BeNull();
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void UpdateUserStatus_Should_Raise_Patched()
    {
        var state = Authenticated();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.UpdateUserStatus(new ChangeUserStatusSelfRequest
        {
            NewStatus = SelfAssignableUserStatus.SelfSuspended
        });

        state.Identity!.UserStatus.Should().Be(UserStatus.SelfSuspended);

        reasons.Should().ContainSingle().Which.Should().Be(UAuthStateChangeReason.Patched);
    }

    [Fact]
    public void UpdateUserStatus_Should_Do_Nothing_When_Anonymous()
    {
        var state = UAuthState.Anonymous();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.UpdateUserStatus(new ChangeUserStatusSelfRequest
        {
            NewStatus = SelfAssignableUserStatus.SelfSuspended
        });

        state.Identity.Should().BeNull();
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void MarkStale_Should_Require_Validation()
    {
        var state = Authenticated();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.MarkStale();

        state.IsStale.Should().BeTrue();
        state.NeedsValidation.Should().BeTrue();

        reasons.Should().ContainSingle()
            .Which.Should().Be(UAuthStateChangeReason.MarkedStale);
    }

    [Fact]
    public void MarkStale_Should_Do_Nothing_When_Anonymous()
    {
        var state = UAuthState.Anonymous();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.MarkStale();

        state.IsStale.Should().BeFalse();
        state.NeedsValidation.Should().BeFalse();
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void MarkValidated_Should_Reset_Stale_And_Update_Timestamp()
    {
        var state = Authenticated();
        state.MarkStale();

        var validatedAt = Now.AddMinutes(5);
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.MarkValidated(validatedAt);

        state.IsStale.Should().BeFalse();
        state.NeedsValidation.Should().BeFalse();
        state.LastValidatedAt.Should().Be(validatedAt);

        reasons.Should().ContainSingle()
            .Which.Should().Be(UAuthStateChangeReason.Validated);
    }

    [Fact]
    public void MarkValidated_Should_Do_Nothing_When_Anonymous()
    {
        var state = UAuthState.Anonymous();
        var reasons = new List<UAuthStateChangeReason>();

        state.Changed += reasons.Add;

        state.MarkValidated(Now);

        state.LastValidatedAt.Should().BeNull();
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void Clear_Should_Remove_Identity_Claims_And_Permissions()
    {
        var state = Authenticated("users.read");
        state.MarkStale();

        var reasons = new List<UAuthStateChangeReason>();
        state.Changed += reasons.Add;

        state.Clear();

        state.IsAuthenticated.Should().BeFalse();
        state.Identity.Should().BeNull();
        state.IsStale.Should().BeFalse();
        state.NeedsValidation.Should().BeFalse();

        state.HasPermission("users.read").Should().BeFalse();
        state.HasAnyPermission("users.read").Should().BeFalse();
        state.IsInRole("Member").Should().BeFalse();

        reasons.Should().ContainSingle()
            .Which.Should().Be(UAuthStateChangeReason.Cleared);
    }

    [Fact]
    public void New_Snapshot_Should_Replace_Previous_User_Permissions()
    {
        var state = UAuthState.Anonymous();

        state.ApplySnapshot(
            Snapshot("user-1", permissions: ["users.delete"]),
            Now);

        state.HasPermission("users.delete").Should().BeTrue();

        state.Clear();

        state.ApplySnapshot(
            Snapshot("user-2", permissions: ["users.read"]),
            Now.AddMinutes(1));

        state.Identity!.UserKey.Should().Be(UserKey.FromString("user-2"));

        state.HasPermission("users.read").Should().BeTrue();
        state.HasPermission("users.delete").Should().BeFalse();
    }

    [Theory]
    [InlineData("users.read", true)]
    [InlineData("users.write", false)]
    [InlineData("users.delete", false)]
    public void HasPermission_Should_Check_Exact_Permissions(
        string permission,
        bool expected)
    {
        var state = Authenticated("users.read");

        state.HasPermission(permission).Should().Be(expected);
    }

    [Fact]
    public void HasAnyPermission_Should_Return_True_If_Any_Matches()
    {
        var state = Authenticated("users.read");

        state.HasAnyPermission(
            "users.delete",
            "users.read",
            "users.write").Should().BeTrue();
    }

    [Fact]
    public void HasAnyPermission_Should_Return_False_When_None_Match()
    {
        var state = Authenticated("users.read");

        state.HasAnyPermission(
            "users.delete",
            "users.write").Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_Should_Return_False_For_Empty_Input()
    {
        var state = Authenticated("users.read");

        state.HasAnyPermission().Should().BeFalse();
    }

    [Fact]
    public void Touch_Without_State_Update_Should_Only_Request_Render()
    {
        var state = Authenticated();
        var renders = 0;
        var reasons = new List<UAuthStateChangeReason>();

        state.RequestRender = () => renders++;
        state.Changed += reasons.Add;

        state.Touch(updateState: false);

        renders.Should().Be(1);
        state.IsStale.Should().BeFalse();
        reasons.Should().BeEmpty();
    }

    [Fact]
    public void Touch_With_State_Update_Should_Mark_Stale_And_Request_Render()
    {
        var state = Authenticated();
        var renders = 0;

        state.RequestRender = () => renders++;

        state.Touch();

        renders.Should().Be(1);
        state.IsStale.Should().BeTrue();
        state.NeedsValidation.Should().BeTrue();
    }

    [Fact]
    public void Anonymous_ToClaimsPrincipal_Should_Not_Be_Authenticated()
    {
        var principal = UAuthState.Anonymous().ToClaimsPrincipal();

        principal.Identity!.IsAuthenticated.Should().BeFalse();
        principal.Claims.Should().BeEmpty();
    }

    [Fact]
    public void Authenticated_ToClaimsPrincipal_Should_Contain_Identity_And_Roles()
    {
        var state = Authenticated("users.read");

        var principal = state.ToClaimsPrincipal();

        principal.Identity!.IsAuthenticated.Should().BeTrue();

        principal.FindFirst(ClaimTypes.NameIdentifier)!.Value
            .Should().Be("user-1");

        principal.FindFirst(ClaimTypes.Name)!.Value
            .Should().Be("user-1");

        principal.IsInRole("Member").Should().BeTrue();
    }

    [Theory]
    [InlineData("users.read", true)]
    [InlineData("users.create", true)]
    [InlineData("users.delete", true)]
    [InlineData("orders.read", false)]
    public void HasPermission_Should_Resolve_Wildcard_Permissions(string permission, bool expected)
    {
        var state = Authenticated("users.*");
        state.HasPermission(permission).Should().Be(expected);
    }

    [Fact]
    public void HasAnyPermission_Should_Reject_Unrelated_Permissions()
    {
        var state = Authenticated("users.*");
        state.HasAnyPermission("orders.read", "products.create").Should().BeFalse();
    }
}

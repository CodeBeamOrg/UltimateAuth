using Bunit;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthStateViewTests : BunitContext
{
    private const string AuthorizedContent = "AUTH-CONTENT";
    private const string DeniedContent = "DENIED-CONTENT";
    private const string InactiveContent = "INACTIVE-CONTENT";

    private readonly Mock<IAuthorizationService> _authorization = new();

    public UAuthStateViewTests()
    {
        Services.AddSingleton(_authorization.Object);
    }

    // ============================================================
    // Authentication
    // ============================================================

    [Fact]
    public void AnonymousUser_RendersNotAuthorized()
    {
        var state = UAuthState.Anonymous();

        var cut = RenderView(state);

        AssertDenied(cut.Markup);
    }

    [Theory]
    [InlineData(AuthorizationMatchMode.Any)]
    [InlineData(AuthorizationMatchMode.All)]
    [InlineData(AuthorizationMatchMode.Category)]
    public void AnonymousUser_WithoutRequirements_IsNotAuthorized(
        AuthorizationMatchMode matchMode)
    {
        var state = UAuthState.Anonymous();

        var cut = RenderView(
            state,
            matchMode: matchMode);

        AssertDenied(cut.Markup);
    }

    [Theory]
    [InlineData(AuthorizationMatchMode.Any)]
    [InlineData(AuthorizationMatchMode.All)]
    [InlineData(AuthorizationMatchMode.Category)]
    public void AuthenticatedUser_WithoutRequirements_IsAuthorized(
        AuthorizationMatchMode matchMode)
    {
        var state = AuthenticatedState();

        var cut = RenderView(
            state,
            matchMode: matchMode);

        AssertAuthorized(cut.Markup);
    }

    // ============================================================
    // Roles
    // ============================================================

    [Fact]
    public void MatchingRole_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"]);

        var cut = RenderView(
            state,
            roles: "Admin");

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void MissingRole_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["User"]);

        var cut = RenderView(
            state,
            roles: "Admin");

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void RolesCsv_TrimsWhitespaceAndIgnoresEmptyEntries()
    {
        var state = AuthenticatedState(
            roles: ["Admin"]);

        var cut = RenderView(
            state,
            roles: "  User, , Admin,   ");

        AssertAuthorized(cut.Markup);
    }

    // ============================================================
    // Permissions
    // ============================================================

    [Fact]
    public void MatchingPermission_RendersAuthorized()
    {
        var state = AuthenticatedState(
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            permissions: "users.read");

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void MissingPermission_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            permissions: ["users.list"]);

        var cut = RenderView(
            state,
            permissions: "users.read");

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void PermissionsCsv_TrimsWhitespaceAndIgnoresEmptyEntries()
    {
        var state = AuthenticatedState(
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            permissions: " users.write, , users.read, ");

        AssertAuthorized(cut.Markup);
    }

    // ============================================================
    // Any
    // ============================================================

    [Fact]
    public void Any_WhenOneRoleMatches_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"]);

        var cut = RenderView(
            state,
            roles: "User,Admin",
            matchMode: AuthorizationMatchMode.Any);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Any_WhenOnePermissionMatches_RendersAuthorized()
    {
        var state = AuthenticatedState(
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            permissions: "users.write,users.read",
            matchMode: AuthorizationMatchMode.Any);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Any_WhenRoleFailsButPermissionMatches_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["User"],
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            roles: "Admin",
            permissions: "users.read",
            matchMode: AuthorizationMatchMode.Any);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Any_WhenNothingMatches_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["User"],
            permissions: ["users.list"]);

        var cut = RenderView(
            state,
            roles: "Admin",
            permissions: "users.read",
            matchMode: AuthorizationMatchMode.Any);

        AssertDenied(cut.Markup);
    }

    // ============================================================
    // All
    // ============================================================

    [Fact]
    public void All_WhenAllValuesMatch_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin", "Manager"],
            permissions: ["users.read", "users.write"]);

        var cut = RenderView(
            state,
            roles: "Admin,Manager",
            permissions: "users.read,users.write",
            matchMode: AuthorizationMatchMode.All);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void All_WhenOneRoleFails_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            roles: "Admin,Manager",
            permissions: "users.read",
            matchMode: AuthorizationMatchMode.All);

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void All_WhenOnePermissionFails_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            roles: "Admin",
            permissions: "users.read,users.write",
            matchMode: AuthorizationMatchMode.All);

        AssertDenied(cut.Markup);
    }

    // ============================================================
    // Category
    // ============================================================

    [Fact]
    public void Category_WhenAtLeastOneValueFromEachCategoryMatches_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            roles: "User,Admin",
            permissions: "users.write,users.read",
            matchMode: AuthorizationMatchMode.Category);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Category_WhenRoleCategoryFails_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["User"],
            permissions: ["users.read"]);

        var cut = RenderView(
            state,
            roles: "Admin,Manager",
            permissions: "users.read",
            matchMode: AuthorizationMatchMode.Category);

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void Category_WhenPermissionCategoryFails_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.list"]);

        var cut = RenderView(
            state,
            roles: "Admin",
            permissions: "users.read,users.write",
            matchMode: AuthorizationMatchMode.Category);

        AssertDenied(cut.Markup);
    }

    // ============================================================
    // Policy
    // ============================================================

    [Fact]
    public void Policy_WhenSucceeded_RendersAuthorized()
    {
        var state = AuthenticatedState();

        SetupPolicy(
            "CanManageUsers",
            AuthorizationResult.Success());

        var cut = RenderView(
            state,
            policy: "CanManageUsers");

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Policy_WhenFailed_RendersNotAuthorized()
    {
        var state = AuthenticatedState();

        SetupPolicy(
            "CanManageUsers",
            AuthorizationResult.Failed());

        var cut = RenderView(
            state,
            policy: "CanManageUsers");

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void Category_WhenRoleAndPermissionMatchButPolicyFails_RendersNotAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        SetupPolicy(
            "CanManageUsers",
            AuthorizationResult.Failed());

        var cut = RenderView(
            state,
            roles: "Admin",
            permissions: "users.read",
            policy: "CanManageUsers",
            matchMode: AuthorizationMatchMode.Category);

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void Any_WhenRoleFailsButPolicySucceeds_RendersAuthorized()
    {
        var state = AuthenticatedState(
            roles: ["User"]);

        SetupPolicy(
            "CanManageUsers",
            AuthorizationResult.Success());

        var cut = RenderView(
            state,
            roles: "Admin",
            policy: "CanManageUsers",
            matchMode: AuthorizationMatchMode.Any);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void Policy_UsesPrincipalCreatedFromUAuthState()
    {
        var state = AuthenticatedState(
            roles: ["Admin"],
            permissions: ["users.read"]);

        ClaimsPrincipal? receivedPrincipal = null;

        _authorization
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                "CanManageUsers"))
            .Callback<ClaimsPrincipal, object?, string>(
                (principal, _, _) => receivedPrincipal = principal)
            .ReturnsAsync(AuthorizationResult.Success());

        RenderView(
            state,
            policy: "CanManageUsers");

        receivedPrincipal.Should().NotBeNull();
        receivedPrincipal!.Identity!.IsAuthenticated.Should().BeTrue();
        receivedPrincipal.IsInRole("Admin").Should().BeTrue();
        receivedPrincipal.HasClaim(
            "uauth:permission",
            "users.read").Should().BeTrue();
    }

    // ============================================================
    // Session state
    // ============================================================

    [Fact]
    public void InactiveSession_WhenRequireActiveTrue_RendersInactive()
    {
        var state = AuthenticatedState(
            sessionState: SessionState.Revoked);

        var cut = RenderView(
            state,
            requireActive: true);

        AssertInactive(cut.Markup);
    }

    [Fact]
    public void InactiveSession_WithoutInactiveTemplate_FallsBackToNotAuthorized()
    {
        var state = AuthenticatedState(
            sessionState: SessionState.Revoked);

        var cut = RenderView(
            state,
            requireActive: true,
            includeInactive: false);

        AssertDenied(cut.Markup);
    }

    [Fact]
    public void InactiveSession_WhenRequireActiveFalse_RendersAuthorized()
    {
        var state = AuthenticatedState(
            sessionState: SessionState.Revoked);

        var cut = RenderView(
            state,
            requireActive: false);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void NullSessionState_WhenRequireActiveTrue_IsNotConsideredInactive()
    {
        var state = AuthenticatedState(
            sessionState: null);

        var cut = RenderView(
            state,
            requireActive: true);

        AssertAuthorized(cut.Markup);
    }

    [Fact]
    public void ActiveSession_WhenRequireActiveTrue_RendersAuthorized()
    {
        var state = AuthenticatedState(
            sessionState: SessionState.Active);

        var cut = RenderView(
            state,
            requireActive: true);

        AssertAuthorized(cut.Markup);
    }

    // ============================================================
    // Parameter re-evaluation
    // ============================================================

    [Fact]
    public void ChangingRequireActive_ReevaluatesState()
    {
        var state = AuthenticatedState(
            sessionState: SessionState.Revoked);

        var cut = Render<StateViewHost>(parameters => parameters
            .Add(x => x.State, state)
            .Add(x => x.RequireActive, true));

        cut.Markup.Should().Contain(InactiveContent);
        cut.Markup.Should().NotContain(AuthorizedContent);

        cut.Render(parameters => parameters
            .Add(x => x.State, state)
            .Add(x => x.RequireActive, false));

        cut.Markup.Should().Contain(AuthorizedContent);
        cut.Markup.Should().NotContain(InactiveContent);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private IRenderedComponent<CascadingValue<UAuthState>> RenderView(
        UAuthState state,
        string? roles = null,
        string? permissions = null,
        string? policy = null,
        AuthorizationMatchMode matchMode = AuthorizationMatchMode.Category,
        bool requireActive = true,
        bool includeInactive = true)
    {
        return Render<CascadingValue<UAuthState>>(parameters =>
            parameters
                .Add(x => x.Value, state)
                .AddChildContent(builder =>
                {
                    builder.OpenComponent<UAuthStateView>(0);

                    if (roles is not null)
                    {
                        builder.AddAttribute(
                            1,
                            nameof(UAuthStateView.Roles),
                            roles);
                    }

                    if (permissions is not null)
                    {
                        builder.AddAttribute(
                            2,
                            nameof(UAuthStateView.Permissions),
                            permissions);
                    }

                    if (policy is not null)
                    {
                        builder.AddAttribute(
                            3,
                            nameof(UAuthStateView.Policy),
                            policy);
                    }

                    builder.AddAttribute(
                        4,
                        nameof(UAuthStateView.MatchMode),
                        matchMode);

                    builder.AddAttribute(
                        5,
                        nameof(UAuthStateView.RequireActive),
                        requireActive);

                    builder.AddAttribute(
                        6,
                        nameof(UAuthStateView.Authorized),
                        (RenderFragment<UAuthState>)(_ => child =>
                            child.AddContent(0, AuthorizedContent)));

                    builder.AddAttribute(
                        7,
                        nameof(UAuthStateView.NotAuthorized),
                        (RenderFragment)(child =>
                            child.AddContent(0, DeniedContent)));

                    if (includeInactive)
                    {
                        builder.AddAttribute(
                            8,
                            nameof(UAuthStateView.Inactive),
                            (RenderFragment<UAuthState>)(_ => child =>
                                child.AddContent(0, InactiveContent)));
                    }

                    builder.CloseComponent();
                }));
    }

    private void SetupPolicy(
        string policy,
        AuthorizationResult result)
    {
        _authorization
            .Setup(x => x.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                policy))
            .ReturnsAsync(result);
    }

    private static void AssertAuthorized(string markup)
    {
        markup.Should().Contain(AuthorizedContent);
        markup.Should().NotContain(DeniedContent);
        markup.Should().NotContain(InactiveContent);
    }

    private static void AssertDenied(string markup)
    {
        markup.Should().Contain(DeniedContent);
        markup.Should().NotContain(AuthorizedContent);
        markup.Should().NotContain(InactiveContent);
    }

    private static void AssertInactive(string markup)
    {
        markup.Should().Contain(InactiveContent);
        markup.Should().NotContain(AuthorizedContent);
        markup.Should().NotContain(DeniedContent);
    }

    private static UAuthState AuthenticatedState(
        string[]? roles = null,
        string[]? permissions = null,
        SessionState? sessionState = SessionState.Active)
    {
        var claims = new List<(string Type, string Value)>();

        foreach (var role in roles ?? [])
            claims.Add((ClaimTypes.Role, role));

        foreach (var permission in permissions ?? [])
            claims.Add(("uauth:permission", permission));

        var snapshot = new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = UserKey.FromGuid(Guid.NewGuid()),
                Tenant = TenantKey.FromExternal("tenant-a"),
                PrimaryUserName = "alice",
                DisplayName = "Alice",
                UserStatus = UserStatus.Active,
                SessionState = sessionState
            },
            Claims = ClaimsSnapshot.From(claims.ToArray())
        };

        var state = UAuthState.Anonymous();

        state.ApplySnapshot(
            snapshot,
            DateTimeOffset.UtcNow);

        return state;
    }

    private sealed class StateViewHost : ComponentBase
    {
        [Parameter]
        public UAuthState State { get; set; } = default!;

        [Parameter]
        public bool RequireActive { get; set; }

        protected override void BuildRenderTree(
            RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<UAuthState>>(0);

            builder.AddAttribute(
                1,
                nameof(CascadingValue<UAuthState>.Value),
                State);

            builder.AddAttribute(
                2,
                nameof(CascadingValue<UAuthState>.ChildContent),
                (RenderFragment)(content =>
                {
                    content.OpenComponent<UAuthStateView>(0);

                    content.AddAttribute(
                        1,
                        nameof(UAuthStateView.RequireActive),
                        RequireActive);

                    content.AddAttribute(
                        2,
                        nameof(UAuthStateView.Authorized),
                        (RenderFragment<UAuthState>)(_ => child =>
                            child.AddContent(0, AuthorizedContent)));

                    content.AddAttribute(
                        3,
                        nameof(UAuthStateView.NotAuthorized),
                        (RenderFragment)(child =>
                            child.AddContent(0, DeniedContent)));

                    content.AddAttribute(
                        4,
                        nameof(UAuthStateView.Inactive),
                        (RenderFragment<UAuthState>)(_ => child =>
                            child.AddContent(0, InactiveContent)));

                    content.CloseComponent();
                }));

            builder.CloseComponent();
        }
    }
}
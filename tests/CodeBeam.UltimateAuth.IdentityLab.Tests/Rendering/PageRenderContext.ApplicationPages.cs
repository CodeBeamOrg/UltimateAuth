using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Contracts;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

internal sealed partial class PageRenderContext
{
    public static UserKey RenderUser => UserKey.FromString("render-user");

    public void AddManagementServices()
    {
        AddLabServices();
        var summaries = new Mock<IUserSummaryQueryStore>(MockBehavior.Strict);
        summaries.Setup(x => x.GetByKeysAsync(It.IsAny<IReadOnlyCollection<UserKey>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserSummary
            {
                UserKey = RenderUser, DisplayName = "Render Person", UserName = "render-person",
                PrimaryEmail = "render@example.test", Status = UserStatus.Active
            } });
        var summaryFactory = new Mock<IUserSummaryQueryStoreFactory>(MockBehavior.Strict);
        summaryFactory.Setup(x => x.Create(TenantKeys.Single)).Returns(summaries.Object);
        Services.AddSingleton(summaryFactory.Object);
        Services.AddScoped<SandboxAccessService>();
        Services.AddScoped<LabProvisioningRollbackGate>();
        Services.AddScoped<LabUserProvisioningService>();
        Services.AddScoped<SandboxUserService>();
        Services.AddScoped<SandboxRepairService>();
        Services.AddScoped<SandboxAccountDeletionService>();
        var product = new Mock<IUAuthProductInfoProvider>(MockBehavior.Strict);
        product.Setup(x => x.Get()).Returns(new UAuthProductInfo { Version = "render-test" });
        Services.AddSingleton(product.Object);
    }

    public void SetupProfileAndAccess()
    {
        Users.Setup(x => x.GetMeAsync(It.IsAny<GetProfileRequest>()))
            .ReturnsAsync(Success(new UserView
            {
                UserKey = RenderUser, DisplayName = "Render Person", UserName = "render-person",
                PrimaryEmail = "render@example.test", Status = UserStatus.Active
            }));
        Identifiers.Setup(x => x.GetMyAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(Success(Empty<UserIdentifierInfo>()));
        Sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(Success(Empty<SessionChainSummary>()));
        Authorization.Setup(x => x.GetMyRolesAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(Success(new UserRolesGetResult { UserKey = RenderUser, Roles = Empty<UserRoleInfo>() }));
        Authorization.Setup(x => x.GetMyPermissionsAsync())
            .ReturnsAsync(Success(new UserPermissionsResult { Permissions = Array.Empty<Permission>() }));
    }

    public void AddAdminServices()
    {
        AddManagementServices();
        var role = Role.Create(LabRoleIds.Admin, TenantKeys.Single, "Ladmin", null, DateTimeOffset.UtcNow);
        var roles = new Mock<IRoleStore>(MockBehavior.Strict);
        roles.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<RoleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { role });
        var roleFactory = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        roleFactory.Setup(x => x.Create(TenantKeys.Single)).Returns(roles.Object);
        var assignments = new Mock<IUserRoleStore>(MockBehavior.Strict);
        assignments.Setup(x => x.GetAssignmentsAsync(RenderUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new UserRole { Tenant = TenantKeys.Single, UserKey = RenderUser, RoleId = role.Id } });
        var assignmentFactory = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        assignmentFactory.Setup(x => x.Create(TenantKeys.Single)).Returns(assignments.Object);
        Services.AddSingleton(roleFactory.Object);
        Services.AddSingleton(assignmentFactory.Object);
        Services.AddSingleton(Mock.Of<ILabAdminGuard>());
        Services.AddScoped<LabAdminService>();
        Services.AddScoped<LabRepairService>();
    }

    public void AddDemoStoreServices()
    {
        AddLabServices();
        var flow = new Mock<IAuthFlow>(MockBehavior.Strict);
        flow.Setup(x => x.BeginAsync(It.IsAny<AuthFlowType>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Contexts.AuthenticatedFlow(RenderUser));
        var contexts = new Mock<IAccessContextFactory>(MockBehavior.Strict);
        contexts.Setup(x => x.CreateAsync(It.IsAny<AuthFlowContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<IDictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Contexts.Create(LabDemoPermissions.ProductsRead, RenderUser));
        Services.AddSingleton(flow.Object);
        Services.AddSingleton(contexts.Object);
        // An active sandbox member without demo role assignments is a valid empty-access state.
        var assignments = new Mock<IUserRoleStore>(MockBehavior.Strict);
        assignments.Setup(x => x.GetAssignmentsAsync(RenderUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserRole>());
        var factory = new Mock<IUserRoleStoreFactory>(MockBehavior.Strict);
        factory.Setup(x => x.Create(TenantKeys.Single)).Returns(assignments.Object);
        Services.AddSingleton(factory.Object);
        var roles = new Mock<IRoleStore>(MockBehavior.Strict);
        roles.Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<RoleId>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Role>());
        var roleFactory = new Mock<IRoleStoreFactory>(MockBehavior.Strict);
        roleFactory.Setup(x => x.Create(TenantKeys.Single)).Returns(roles.Object);
        Services.AddSingleton(roleFactory.Object);
        Services.AddScoped<LabDemoStoreService>();
    }

    public void AddDiagnosticsServices()
    {
        Services.AddSingleton(new UAuthClientDiagnostics());
        Services.AddScoped<LabSessionDiagnosticsPolicyService>();
        var chainId = SessionChainId.New();
        var sessionId = AuthSessionId.Parse(new string('s', 32));
        var now = DateTimeOffset.UtcNow;
        var session = new SessionInfo(sessionId, now.AddMinutes(-1), now.AddHours(1), false);
        var chain = new SessionChainSummary
        {
            ChainId = chainId, ActiveSessionId = sessionId, IsCurrentDevice = true,
            State = SessionChainState.Active, CreatedAt = session.CreatedAt, LastSeenAt = now
        };
        Sessions.Setup(x => x.GetMyChainsAsync(It.IsAny<PageRequest>()))
            .ReturnsAsync(Success(new PagedResult<SessionChainSummary>(new[] { chain }, 1, 1, 20, null, false)));
        Sessions.Setup(x => x.GetMyChainDetailAsync(chainId))
            .ReturnsAsync(Success(new SessionChainDetail
            {
                ChainId = chainId, ActiveSessionId = sessionId, State = SessionChainState.Active,
                CreatedAt = session.CreatedAt, LastSeenAt = now, Sessions = new[] { session }
            }));
    }
}

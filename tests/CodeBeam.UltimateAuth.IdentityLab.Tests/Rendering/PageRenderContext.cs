using System.Reflection;
using System.Security.Claims;
using Bunit;
using CodeBeam.UltimateAuth.Authorization;
using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Rendering;

internal sealed partial class PageRenderContext : BunitContext
{
    public Mock<IUAuthClient> Client { get; } = new(MockBehavior.Strict);
    public Mock<ISessionClient> Sessions { get; } = new(MockBehavior.Strict);
    public Mock<IAuthorizationClient> Authorization { get; } = new(MockBehavior.Strict);
    public Mock<IUserIdentifierClient> Identifiers { get; } = new(MockBehavior.Strict);
    public Mock<IUserClient> Users { get; } = new(MockBehavior.Strict);

    public PageRenderContext()
    {
        // MudBlazor's DOM measurement/focus calls have no browser in bUnit.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLogging();
        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddAuthorizationCore();
        // Keep the real internal evaluator, without installing the production client host.
        var registrations = new ServiceCollection();
        registrations.AddUltimateAuthClientBlazor();
        ((IServiceCollection)Services).Add(registrations.Single(x => x.ServiceType == typeof(IUAuthAuthorizationEvaluator)));
        Client.SetupGet(x => x.Sessions).Returns(Sessions.Object);
        Client.SetupGet(x => x.Authorization).Returns(Authorization.Object);
        Client.SetupGet(x => x.Users).Returns(Users.Object);
        Client.SetupGet(x => x.Identifiers).Returns(Identifiers.Object);
        Services.AddSingleton(Client.Object);
        Services.AddSingleton(Options.Create(new UAuthClientOptions()));
        Services.AddSingleton(Options.Create(new UAuthServerOptions()));
        var device = new Mock<IDeviceIdProvider>();
        device.Setup(x => x.GetOrCreateAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<DeviceId>(DeviceId.Create("render-test-device-0001")));
        Services.AddSingleton(device.Object);
        Services.AddSingleton(Mock.Of<IHubCredentialResolver>());
        Services.AddSingleton(Mock.Of<IHubFlowReader>());
        Services.AddSingleton(Mock.Of<IHubCapabilities>());
    }

    public IRenderedComponent<T> RenderPage<T>(UAuthState? state = null) where T : IComponent
    {
        // Register page-specific dependencies before the first render freezes DI.
        Render<MudPopoverProvider>();
        var authState = state ?? UAuthState.Anonymous();
        return Render<CascadingValue<Task<Microsoft.AspNetCore.Components.Authorization.AuthenticationState>>>(p => p
            .Add(x => x.Value, Task.FromResult(new Microsoft.AspNetCore.Components.Authorization.AuthenticationState(authState.ToClaimsPrincipal())))
            .AddChildContent<CascadingValue<UAuthState>>(child => child
                .Add(x => x.Value, authState)
                .AddChildContent<T>())).FindComponent<T>();
    }

    public static UAuthState Authenticated(string role, params string[] permissions)
    {
        var state = UAuthState.Anonymous();
        // Use the actual snapshot lifecycle; no production friend assembly or API changes.
        typeof(UAuthState).GetMethod("ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(state, [new AuthStateSnapshot
            {
                Identity = new AuthIdentitySnapshot
                {
                    UserKey = UserKey.FromString("render-user"),
                    Tenant = TenantKeys.Single,
                    SessionState = SessionState.Active,
                    UserStatus = UserStatus.Active
                },
                Claims = ClaimsSnapshot.From(new[] { (Type: ClaimTypes.Role, Value: role) }
                    .Concat(permissions.Select(permission => (Type: "uauth:permission", Value: permission))).ToArray())
            }, DateTimeOffset.UtcNow]);
        return state;
    }

    public void AddLabServices()
    {
        // Only LabDbContext uses this isolated store. No server host, migrations or PostgreSQL.
        var databaseName = Guid.NewGuid().ToString();
        Services.AddDbContextFactory<LabDbContext>(o => o.UseInMemoryDatabase(databaseName));
        Services.AddSingleton(Mock.Of<IAuthFlow>());
        Services.AddSingleton(Mock.Of<IAccessContextFactory>());
        Services.AddSingleton(Mock.Of<IUserApplicationService>());
        Services.AddSingleton(Mock.Of<IUserIdentifierAvailabilityService>());
        Services.AddSingleton(Mock.Of<IRoleStoreFactory>());
        Services.AddSingleton(Mock.Of<IUserRoleStoreFactory>());
        Services.AddSingleton(Mock.Of<IUserLifecycleStoreFactory>());
        Services.AddScoped<LabAccessContextProvider>();
        Services.AddScoped<SandboxService>();
        Services.AddScoped<ActivityLogService>();
        Services.AddScoped<LabRegistrationService>();
        Services.AddScoped<LabRegistrationAvailabilityService>();
        Services.AddScoped<LabDashboardService>();
        Services.AddScoped<LabOwnerRepairService>();
    }

    public async Task SeedSandboxAsync(bool owner)
    {
        await using var db = await Services.GetRequiredService<IDbContextFactory<LabDbContext>>().CreateDbContextAsync();
        var sandbox = new Sandbox
        {
            Name = "Render sandbox",
            OwnerUserKey = UserKey.FromString(owner ? "render-user" : "other-owner"),
            Settings = new SandboxSettings { MaxUsers = 5 }
        };
        sandbox.Members.Add(new SandboxMember
        {
            UserKey = UserKey.FromString("render-user"),
            Role = owner ? SandboxMemberRole.Owner : SandboxMemberRole.Member
        });
        db.Sandboxes.Add(sandbox);
        await db.SaveChangesAsync();
    }

    public static UAuthResult<T> Success<T>(T value) => new() { IsSuccess = true, Status = 200, Value = value };
    public static PagedResult<T> Empty<T>() => new(Array.Empty<T>(), 0, 1, 20, null, false);
}

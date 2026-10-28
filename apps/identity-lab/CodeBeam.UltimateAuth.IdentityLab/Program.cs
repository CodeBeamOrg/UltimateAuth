using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.IdentityLab.Authorization;
using CodeBeam.UltimateAuth.IdentityLab.Components;
using CodeBeam.UltimateAuth.IdentityLab.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Endpoints;
using CodeBeam.UltimateAuth.IdentityLab.Persistence;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Seed;
using CodeBeam.UltimateAuth.IdentityLab.Services;
using CodeBeam.UltimateAuth.Sample.Shared.Infrastructure;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebApplication.CreateBuilder(args);

// TODO(Part 3): Protect system roles through UltimateAuth request guards.
// TODO(Part 3): Add atomic provisioning and recovery for partial failures.
// TODO(Part 3): Enforce sandbox ownership and user limits across all applicable UltimateAuth operations.
// TODO(Part 3): Implement authenticated sandbox repair.

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
    //.AddCircuitOptions(o => o.DetailedErrors = true);

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();
builder.Services.AddScoped<DarkModeManager>();

var connectionString = builder.Configuration.GetConnectionString("IdentityLab")
    ?? throw new InvalidOperationException("Connection string 'IdentityLab' is missing.");

var activityEventBridge = new LabActivityEventBridge();
builder.Services.AddSingleton(activityEventBridge);

builder.Services.AddUltimateAuthServer(o =>
{
    // Identity Lab uses its own trusted creation align with Sandbox management.
    o.Endpoints.DisabledActions.Add(UAuthActions.Users.CreateAnonymous);
    o.Diagnostics.EnableRefreshDetails = true;
    //o.Session.MaxLifetime = TimeSpan.FromSeconds(32);
    //o.Session.Lifetime = TimeSpan.FromSeconds(32);
    //o.Session.TouchInterval = TimeSpan.FromSeconds(9);
    //o.Session.IdleTimeout = TimeSpan.FromSeconds(15);
    //o.Token.AccessTokenLifetime = TimeSpan.FromSeconds(30);
    //o.Token.RefreshTokenLifetime = TimeSpan.FromSeconds(32);
    o.Login.MaxFailedAttempts = 5;
    o.Login.LockoutDuration = TimeSpan.FromMinutes(1);
    o.Identifiers.Behavior.AllowMultipleUsernames = true;


    o.Events.OnUserLoggedIn = context => activityEventBridge.RecordAsync(context.UserKey, ActivityEvents.Login);
    o.Events.OnUserLoggedOut = context => activityEventBridge.RecordAsync(context.UserKey, ActivityEvents.Logout);
})
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseNpgsql(connectionString,
            options => options.MigrationsAssembly(typeof(Program).Assembly.GetName().Name));
    });

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    //o.AutoRefresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    o.Resource.Credentials = UAuthResourceCredentialsMode.SameOrigin;
});


builder.Services.AddDbContextFactory<LabDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.MigrationsAssembly(typeof(LabDbContext).Assembly.GetName().Name);
        npgsql.MigrationsHistoryTable("__IdentityLabMigrationsHistory");
    });
});

builder.Services.AddScoped<ILabAdminGuard, LabAdminGuard>();
builder.Services.AddScoped<LabAccessContextProvider>();
builder.Services.AddScoped<LabAdminAuthorizationFilter>();
builder.Services.AddScoped<LabAdminService>();
builder.Services.AddScoped<SandboxService>();
builder.Services.AddScoped<SandboxAdminService>();
builder.Services.AddScoped<SandboxAccessService>();
builder.Services.AddScoped<LabRegistrationService>();
builder.Services.AddScoped<LabDashboardService>();
builder.Services.AddScoped<SandboxUserService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<LabUserProvisioningService>();
builder.Services.AddScoped<SandboxAccountDeletionService>();
builder.Services.AddScoped<LabRepairService>();
builder.Services.AddScoped<LabOwnerRepairService>();
builder.Services.AddScoped<SandboxRepairService>();
builder.Services.AddScoped<LabDemoRoleService>();
builder.Services.AddScoped<LabDemoStoreService>();
builder.Services.AddScoped<IAccessPolicy, SandboxUAuthAccessPolicy>();
builder.Services.AddScoped<IAccessPolicy, LabApplicationPolicy>();
builder.Services.AddScoped<LabActivityEventHandler>();
builder.Services.AddScoped<LabProvisioningRollbackGate>();

builder.Services.Configure<LabAdminOptions>(builder.Configuration.GetSection(LabAdminOptions.SectionName));

builder.Services.AddScoped<ILabRootResolver, LabRootResolver>();

builder.Services.AddScoped<SeedRunner>();
builder.Services.AddScoped<ISeedContributor, LabAdminSeedContributor>();
builder.Services.AddScoped<ISeedContributor, LabDemoRoleSeedContributor>();
builder.Services.AddLogging();

var app = builder.Build();

activityEventBridge.Initialize(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapLabAdminEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());

await app.SeedIdentityLabAsync();

app.Run();

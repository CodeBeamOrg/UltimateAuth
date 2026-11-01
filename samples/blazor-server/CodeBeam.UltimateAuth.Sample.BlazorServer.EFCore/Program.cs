using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore;
using CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Components;
using CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore.Infrastructure;
using CodeBeam.UltimateAuth.Sample.Seed.Extensions;
using CodeBeam.UltimateAuth.Sample.Shared.Infrastructure;
using CodeBeam.UltimateAuth.Sample.Shared.ResourceApi;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Users;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MudExtensions.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

#region Core

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddOpenApi();

#endregion

# region UI & MudBlazor & Extensions

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();
builder.Services.AddScoped<DarkModeManager>();

#endregion


builder.Services.AddUltimateAuthServer(o =>
{
    o.Diagnostics.EnableRefreshDetails = true;
    //o.Session.MaxLifetime = TimeSpan.FromSeconds(32);
    //o.Session.Lifetime = TimeSpan.FromSeconds(32);
    //o.Session.TouchInterval = TimeSpan.FromSeconds(9);
    //o.Session.IdleTimeout = TimeSpan.FromSeconds(15);
    //o.Token.AccessTokenLifetime = TimeSpan.FromSeconds(30);
    //o.Token.RefreshTokenLifetime = TimeSpan.FromSeconds(32);
    o.Login.MaxFailedAttempts = 2;
    o.Login.LockoutDuration = TimeSpan.FromSeconds(10);
    o.Identifiers.Behavior.AllowMultipleUsernames = true;
})
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauth.db", x => x.MigrationsAssembly("CodeBeam.UltimateAuth.Sample.BlazorServer.EFCore"));
    });

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    //o.AutoRefresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    //o.UAuthStateRefreshMode = UAuthStateRefreshMode.Validate;
});

// Customize UltimateAuth
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<ICredentialResetNotifier, DevelopmentCredentialResetNotifier>();
    builder.Services.AddScoped<IUserIdentifierVerifier, DevelopmentBypassUserIdentifierVerifier>();
}

builder.Services.AddScopedUltimateAuthSampleSeed();
builder.Services.AddScoped<SampleProductApiService>();

// Reverse proxy deployments (Caddy, Nginx, etc.):
// Configure forwarded headers only when running behind a trusted proxy.
//
// builder.Services.Configure<ForwardedHeadersOptions>(options =>
// {
//     options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
//
//     // Replace with the actual trusted proxy address.
//     options.KnownProxies.Add(IPAddress.Parse("192.0.2.10"));
// });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    using (var scope = app.Services.CreateScope())
    {
        await UAuthDbInitializer.InitializeAsync(app.Services, reset: true);
    }
    await app.SeedUltimateAuthAsync();
}

// Enable only when trusted forwarded headers are configured.
// Must run before authentication, rate limiting and HTTPS redirection.
// app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());

app.Run();

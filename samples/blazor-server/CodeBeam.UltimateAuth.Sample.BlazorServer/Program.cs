using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;
using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Credentials.InMemory.Extensions;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Sample.BlazorServer.Components;
using CodeBeam.UltimateAuth.Sample.BlazorServer.Infrastructure;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.InMemory.Extensions;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;
using MudExtensions.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

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
    o.Identifiers.AllowMultipleUsernames = true;
})
    .AddUltimateAuthUsersInMemory()
    .AddUltimateAuthUsersReference()
    .AddUltimateAuthCredentialsInMemory()
    .AddUltimateAuthCredentialsReference()
    .AddUltimateAuthAuthorizationInMemory()
    .AddUltimateAuthAuthorizationReference()
    .AddUltimateAuthInMemorySessions()
    .AddUltimateAuthInMemoryTokens()
    .AddUltimateAuthInMemoryAuthenticationSecurity()
    .AddUltimateAuthArgon2();

builder.Services.AddUltimateAuthClient(o =>
{
    //o.AutoRefresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    //o.UAuthStateRefreshMode = UAuthStateRefreshMode.Validate;
});

builder.Services.AddScoped<DarkModeManager>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;
});

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

    using var scope = app.Services.CreateScope();
    var seedRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
    await seedRunner.RunAsync(null);
}

app.UseForwardedHeaders();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthClientRoutes(typeof(UAuthClientMarker).Assembly);

app.Run();

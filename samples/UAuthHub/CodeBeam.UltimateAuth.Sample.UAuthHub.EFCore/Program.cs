using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.EntityFrameworkCore;
using CodeBeam.UltimateAuth.Sample.Seed.Extensions;
using CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore;
using CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore.Components;
using CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore.Infrastructure;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.EntityFrameworkCore;
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

builder.Services.AddScoped<DarkModeManager>();

builder.Services.AddUltimateAuthServer(o => {
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
    o.UserProfile.EnableMultiProfile = true;
})
    .AddUltimateAuthEntityFrameworkCore(db =>
    {
        db.UseSqlite("Data Source=uauthhub.db", x => x.MigrationsAssembly("CodeBeam.UltimateAuth.Sample.UAuthHub.EFCore"));
    })
    .AddUAuthHub(o => o.AllowedClientOrigins.Add("https://localhost:6132")); // Client sample's URL

builder.Services.AddScopedUltimateAuthSampleSeed();

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    //o.Refresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
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

    using (var scope = app.Services.CreateScope())
    {
        await UAuthDbInitializer.InitializeAsync(app.Services, reset: true);

        var seedRunner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await seedRunner.RunAsync(null);
    }
}

app.UseHttpsRedirection();

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapUAuthHub();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());

app.Run();

using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.IdentityLab.Components;
using CodeBeam.UltimateAuth.Sample.Seed.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddUltimateAuthServer(options =>
{
    options.Login.MaxFailedAttempts = 5;
    options.Login.LockoutDuration = TimeSpan.FromMinutes(1);
    options.Identifiers.Behavior.AllowMultipleUsernames = true;
}).AddUltimateAuthInMemory();
builder.Services.AddUltimateAuthClientBlazor(options =>
    options.Reauth.Behavior = ReauthBehavior.RaiseEvent);
builder.Services.AddUltimateAuthSampleSeed();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    await app.SeedUltimateAuthAsync();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();
app.MapUltimateAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());
app.Run();

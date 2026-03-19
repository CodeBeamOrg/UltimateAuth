using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Sample.UAuthHub.Components;
using CodeBeam.UltimateAuth.Sample.UAuthHub.Infrastructure;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Extensions;
using MudBlazor.Services;
using MudExtensions.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();

//builder.Services.AddAuthorization();

//builder.Services.AddHttpContextAccessor();

builder.Services.AddUltimateAuthServer(o => {
    o.Diagnostics.EnableRefreshDetails = true;
    //o.Session.MaxLifetime = TimeSpan.FromSeconds(32);
    //o.Session.TouchInterval = TimeSpan.FromSeconds(9);
    //o.Session.IdleTimeout = TimeSpan.FromSeconds(15);
})
    .AddInMemoryReference()
    .AddUltimateAuthArgon2();

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    //o.Refresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});

builder.Services.AddSingleton<IUAuthHubMarker, DefaultUAuthHubMarker>();
builder.Services.AddScoped<DarkModeManager>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WasmSample", policy =>
    {
        policy
            .WithOrigins("https://localhost:6130")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("X-UAuth-Refresh"); // TODO: Add exposed headers globally
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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

app.UseHttpsRedirection();
app.UseCors("WasmSample");

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapStaticAssets();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddUltimateAuthRoutes(UAuthAssemblies.BlazorClient());

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        service = "UAuthHub",
        status = "ok"
    });
});

app.Run();

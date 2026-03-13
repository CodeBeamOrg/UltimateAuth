using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Authorization.InMemory;
using CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;
using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Credentials.InMemory.Extensions;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Sample.UAuthHub.Components;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using CodeBeam.UltimateAuth.Users.InMemory.Extensions;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

//builder.Services
//    .AddAuthentication(options =>
//    {
//        options.DefaultAuthenticateScheme = UAuthSchemeDefaults.AuthenticationScheme;
//        options.DefaultSignInScheme = UAuthSchemeDefaults.AuthenticationScheme;
//        options.DefaultChallengeScheme = UAuthSchemeDefaults.AuthenticationScheme;
//    })
//    .AddUAuthCookies();

//builder.Services.AddAuthorization();

//builder.Services.AddHttpContextAccessor();

builder.Services.AddUltimateAuthServer(o => {
    o.Diagnostics.EnableRefreshDetails = true;
    //o.Session.MaxLifetime = TimeSpan.FromSeconds(32);
    //o.Session.TouchInterval = TimeSpan.FromSeconds(9);
    //o.Session.IdleTimeout = TimeSpan.FromSeconds(15);
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
    //o.Refresh.Interval = TimeSpan.FromSeconds(5);
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
});

builder.Services.AddSingleton<IUAuthHubMarker, DefaultUAuthHubMarker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WasmSample", policy =>
    {
        policy
            .WithOrigins("https://localhost:6130")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IUserLifecycleStore>();
    scope.ServiceProvider.GetRequiredService<IUserProfileStore>();
    scope.ServiceProvider.GetRequiredService<IUserIdentifierStore>();

    var seeder = scope.ServiceProvider.GetService<IAuthorizationSeeder>();
    //if (seeder is not null)
    //    await seeder.SeedAsync();

    
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
//app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseCors("WasmSample");

app.UseUltimateAuthWithAspNetCore();
app.UseAntiforgery();

app.MapUltimateAuthEndpoints();
app.MapStaticAssets();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        service = "UAuthHub",
        status = "ok"
    });
});

app.Run();

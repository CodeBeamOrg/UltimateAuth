using CodeBeam.UltimateAuth.Client.BlazorServer;
using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using MudBlazor.Services;
using MudExtensions.Services;
using UltimateAuth.Sample.BlazorServer.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options =>
    {
        options.DetailedErrors = true;
    });

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = UAuthCookieDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = UAuthCookieDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = UAuthCookieDefaults.AuthenticationScheme;
    })
    .AddUAuthCookies();

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddUltimateAuth();

builder.Services.AddUltimateAuthServer(o => {
    o.Diagnostics.EnableRefreshHeaders = true; 
})
    .AddInMemoryCredentials()
    .AddUltimateAuthInMemorySessions()
    .AddUltimateAuthInMemoryTokens()
    .AddUltimateAuthArgon2();

builder.Services.AddUltimateAuthClient();



builder.Services.AddScoped(sp =>
{
    var navigation = sp.GetRequiredService<NavigationManager>();

    return new HttpClient
    {
        BaseAddress = new Uri(navigation.BaseUri)
    };
});

//builder.Services.AddHttpClient("AuthApi", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:7213");
//})
//.ConfigurePrimaryHttpMessageHandler(() =>
//{
//    return new HttpClientHandler
//    {
//        UseCookies = true
//    };
//});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseUltimateAuthServer();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapUAuthEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

using CodeBeam.UltimateAuth.Credentials.InMemory;
using CodeBeam.UltimateAuth.Security.Argon2;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using MudExtensions.Services;
using UltimateAuth.BlazorServer.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();


builder.Services.AddUltimateAuthServer()
    .AddInMemoryCredentials()
    .AddUltimateAuthInMemorySessions()
    .AddUltimateAuthInMemoryTokens()
    .AddUltimateAuthArgon2();

builder.Services.AddHttpClient("AuthApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:7213");
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        UseCookies = true
    };
});


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

using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddUltimateAuth();
builder.Services.AddUltimateAuthClient(o =>
{
    o.Endpoints.BasePath = "https://localhost:6110/auth";
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    o.Login.AllowCredentialPost = true;
});

//builder.Services.AddScoped<AuthenticationStateProvider, UAuthAuthenticationStateProvider>();
//builder.Services.AddScoped<IUAuthAuthenticationStateSource, ClientAuthStateSource>();

//builder.Services.AddAuthorizationCore();

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();

builder.Services.AddScoped<DarkModeManager>();

//builder.Services.AddHttpClient("UAuthHub", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:6110");
//});

//builder.Services.AddHttpClient("ResourceApi", client =>
//{
//    client.BaseAddress = new Uri("https://localhost:6120");
//});

await builder.Build().RunAsync();

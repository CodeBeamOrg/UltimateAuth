using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm;
using MudBlazor.Services;
using MudExtensions.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddUltimateAuth();
builder.Services.AddUltimateAuthClient(o =>
{
    o.Endpoints.Authority = "https://localhost:6110";
});
builder.Services.AddAuthorizationCore();

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

builder.Services.AddHttpClient("UAuthHub", client =>
{
    client.BaseAddress = new Uri("https://localhost:6110");
});

builder.Services.AddHttpClient("ResourceApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:6120");
});

await builder.Build().RunAsync();

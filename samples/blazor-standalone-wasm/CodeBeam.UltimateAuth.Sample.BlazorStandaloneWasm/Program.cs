using CodeBeam.UltimateAuth.Client.Extensions;
using CodeBeam.UltimateAuth.Core.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using CodeBeam.UltimateAuth.Sample.BlazorStandaloneWasm;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddUltimateAuth();
builder.Services.AddUltimateAuthClient();

builder.Services.AddMudBlazorDialog();
builder.Services.AddMudExtensions();

await builder.Build().RunAsync();

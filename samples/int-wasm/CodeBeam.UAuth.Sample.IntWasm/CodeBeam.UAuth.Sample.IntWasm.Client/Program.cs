using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Sample.Shared.Infrastructure;
using CodeBeam.UltimateAuth.Sample.Shared.ResourceApi;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();
builder.Services.AddScoped<DarkModeManager>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<SampleProductApiService>();

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Endpoints.BasePath = "https://localhost:6112/auth"; // UAuthHub EFCore URL
    o.Resource.BaseAddress = "https://localhost:6122";
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    o.Login.AllowCredentialPost = true;
    o.Pkce.ReturnUrl = "https://localhost:6132/home"; // This application domain + path
});

await builder.Build().RunAsync();

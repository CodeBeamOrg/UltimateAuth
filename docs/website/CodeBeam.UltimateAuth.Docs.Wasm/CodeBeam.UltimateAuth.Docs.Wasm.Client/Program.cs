using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddMudServices();
builder.Services.AddMudExtensions();

await builder.Build().RunAsync();

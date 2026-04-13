using CodeBeam.UAuth.Sample.IntWasm.Client.Infrastructure;
using CodeBeam.UAuth.Sample.IntWasm.Client.ResourceApi;
using CodeBeam.UAuth.Sample.IntWasm.Components;
using CodeBeam.UltimateAuth.Client.AspNetCore;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.Core.Domain;
using MudBlazor.Services;
using MudExtensions.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddMudServices(o => {
    o.SnackbarConfiguration.PreventDuplicates = false;
});
builder.Services.AddMudExtensions();

builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.Endpoints.BasePath = "https://localhost:6112/auth"; // UAuthHub EFCore URL
    o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
    o.Login.AllowCredentialPost = true;
    o.Pkce.ReturnUrl = "https://localhost:6132/home"; // This application domain + path
});

builder.Services.AddScoped<DarkModeManager>();

builder.Services.AddUltimateAuthAspNetCoreCompatibility();
// If you want to use the UltimateAuthClientBlazor without the AspNetCore compatibility layer, you can register the services like this instead:
// It gives full AspNetCore compatibility but affects performance significantly.
//builder.Services.AddUltimateAuthResourceApi(o => o.UAuthHubBaseUrl = "https://localhost:6112/auth");

builder.Services.AddScoped<ProductApiService>();

builder.Services.AddHttpClient("resourceApi", client =>
{
    client.BaseAddress = new Uri("https://localhost:6122"); // Resource API URL
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

//app.UseUltimateAuthResourceApiWithAspNetCore();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CodeBeam.UAuth.Sample.IntWasm.Client._Imports).Assembly, UAuthAssemblies.BlazorClient().First());

app.Run();

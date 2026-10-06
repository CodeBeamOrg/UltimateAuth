using Bunit;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudExtensions.Services;

namespace CodeBeam.UltimateAuth.Tests.Unit.Samples;

public abstract class UAuthHubComponentTestBase : BunitContext
{
    protected UAuthHubComponentTestBase()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        Services.AddSingleton<IConfiguration>(configuration);

        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddUltimateAuthServer().AddUltimateAuthInMemory().AddUAuthHub();
        Services.AddUltimateAuthClientBlazor();
    }
}
using Bunit;
using CodeBeam.UltimateAuth.Client.Blazor.Extensions;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudExtensions.Services;
using System.Security.Cryptography;

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
        Services.Configure<UAuthServerOptions>(options =>
        {
            options.Cryptography.JwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            options.Cryptography.TokenHashingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        });
        Services.AddUltimateAuthClientBlazor();
    }
}

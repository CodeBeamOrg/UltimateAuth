using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Authorization.InMemory.Extensions;
using CodeBeam.UltimateAuth.Authorization.Reference.Extensions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.InMemory.Extensions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using CodeBeam.UltimateAuth.Users.InMemory.Extensions;
using CodeBeam.UltimateAuth.Users.Reference;
using CodeBeam.UltimateAuth.Users.Reference.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal sealed class TestAuthRuntime<TUserId> where TUserId : notnull
{
    public IServiceProvider Services { get; }

    public TestAuthRuntime(Action<UAuthServerOptions>? configureServer = null, Action<UAuthOptions>? configureCore = null)
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddUltimateAuth(configureCore ?? (_ => { }));
        services.AddUltimateAuthServer(options =>
        {
            configureServer?.Invoke(options);
        });

        services.AddSingleton<IUAuthPasswordHasher, TestPasswordHasher>();
        // InMemory plugins
        services.AddUltimateAuthUsersInMemory();
        services.AddUltimateAuthCredentialsInMemory();
        services.AddUltimateAuthInMemorySessions();
        services.AddUltimateAuthInMemoryTokens();
        services.AddUltimateAuthInMemoryAuthenticationSecurity();
        services.AddUltimateAuthAuthorizationInMemory();
        services.AddUltimateAuthAuthorizationReference();
        services.AddUltimateAuthUsersReference();

        services.AddScoped<ILoginOrchestrator, LoginOrchestrator>();
        services.AddScoped<IUserRuntimeStateProvider, UserRuntimeStateProvider>();
        services.AddSingleton<InMemoryAuthenticationSecurityStateStore>();
        services.AddSingleton<IAuthenticationSecurityStateStore>(sp =>
            sp.GetRequiredService<InMemoryAuthenticationSecurityStateStore>());

        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        services.AddSingleton<IConfiguration>(configuration);


        Services = services.BuildServiceProvider();
        Services.GetRequiredService<SeedRunner>().RunAsync(null).GetAwaiter().GetResult();
    }

    public ILoginOrchestrator GetLoginOrchestrator()
        => Services.GetRequiredService<ILoginOrchestrator>();

    public ValueTask<AuthFlowContext> CreateLoginFlowAsync(TenantKey? tenant = null)
    {
        var httpContext = TestHttpContext.Create(tenant);
        return Services.GetRequiredService<IAuthFlowContextFactory>().CreateAsync(httpContext, AuthFlowType.Login);
    }

    public IUserApplicationService GetUserApplicationService()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IUserApplicationService>();
    }
}

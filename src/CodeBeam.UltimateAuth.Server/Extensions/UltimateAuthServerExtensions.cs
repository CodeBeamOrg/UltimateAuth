using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class UltimateAuthServerExtensions
    {
        /// <summary>
        /// Registers UltimateAuth Server-side authentication components:
        /// PKCE, Session Issuing, Token Issuing, Login/Logout endpoints, Cookie pipeline.
        /// </summary>
        public static IServiceCollection AddUltimateAuthServer(
            this IServiceCollection services,
            Action<UltimateAuthServerOptions>? configure = null)
        {
            // Options
            var serverOptions = new UltimateAuthServerOptions();
            configure?.Invoke(serverOptions);

            services.AddSingleton(serverOptions);

            services.AddSingleton(serverOptions.Session);
            services.AddSingleton(serverOptions.Tokens);
            services.AddSingleton(serverOptions.Pkce);
            services.AddSingleton(serverOptions.MultiTenant);
            serverOptions.ConfigureServices?.Invoke(services);

            // Authentication Schemes
            services.AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = UltimateAuthDefaults.SessionScheme;
                o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = UltimateAuthDefaults.SessionScheme;
            })
            .AddCookie(UltimateAuthDefaults.SessionScheme, cookie =>
            {
                cookie.Cookie.Name = options.SessionCookieName;
                cookie.Cookie.Path = "/";
                cookie.SlidingExpiration = false; // Session middleware handle edecek
                cookie.HttpOnly = true;
                cookie.SecurePolicy = CookieSecurePolicy.Always;
                cookie.SameSite = SameSiteMode.Strict;
            });

            // PKCE Services
            services.AddScoped<IPkceValidator, PkceValidator>();
            services.AddScoped<IPkceCodeStore, InMemoryPkceCodeStore>();

            // Session Services
            services.AddScoped<ISessionIssuer, SessionIssuer>();
            services.AddScoped<ISessionCookieWriter, SessionCookieWriter>();

            // Token Services
            services.AddScoped<IAccessTokenIssuer, AccessTokenIssuer>();
            services.AddScoped<ITokenSigningService, HmacTokenSigningService>();

            // High-level Authentication Handlers
            services.AddScoped<ILoginService, LoginService>();
            services.AddScoped<ILogoutService, LogoutService>();
            services.AddScoped<IHybridAuthFlow, HybridAuthFlow>();

            // Endpoint Registration (Minimal API)
            services.AddSingleton<IAuthEndpointRegistrar, AuthEndpointRegistrar>();
            services.AddScoped<IPkceEndpointHandler, DefaultPkceEndpointHandler>();
            services.AddScoped<ILoginEndpointHandler, DefaultLoginEndpointHandler>();
            services.AddScoped<ILogoutEndpointHandler, DefaultLogoutEndpointHandler>();
            services.AddScoped<ISessionRefreshEndpointHandler, DefaultSessionRefreshEndpointHandler>();
            services.AddScoped<IReauthEndpointHandler, DefaultReauthEndpointHandler>();
            services.AddScoped<ITokenEndpointHandler, DefaultTokenEndpointHandler>();
            services.AddScoped<ISessionManagementHandler, DefaultSessionManagementHandler>();
            services.AddScoped<IUserInfoEndpointHandler, DefaultUserInfoEndpointHandler>();

            // Middleware dependencies
            services.AddScoped<ISessionPipeline, SessionPipeline>();

            return services;
        }

        /// <summary>
        /// Registers UltimateAuth Server endpoints into application's routing pipeline.
        /// </summary>
        public static void MapUltimateAuthServer(this WebApplication app)
        {
            var options = app.Services.GetRequiredService<UltimateAuthServerOptions>();
            var registrar = app.Services.GetRequiredService<IAuthEndpointRegistrar>();

            var group = app.MapGroup(options.RoutePrefix);

            registrar.MapEndpoints(group, options);

            // developer customization hook
            options.OnConfigureEndpoints?.Invoke(app);
        }

    }
}

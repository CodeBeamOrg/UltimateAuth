using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddUltimateAuthResourceApi(o =>
    {
        o.Auth.Jwt = new UAuthJwtValidationOptions
        {
            Issuer = "UAuth",
            Audience = "UAuthClient",
            ValidAlgorithms = [ SecurityAlgorithms.HmacSha256 ]
        };

        o.Cryptography.JwtVerificationKeys.Add(
            new UAuthJwtVerificationKeyOptions
            {
                KeyId = "uauth-hs256-v1",
                // Development only. Production must use secure key management.
                Key = Convert.ToBase64String(Encoding.UTF8.GetBytes("UltimateAuth-Development-JWT-Signing-Key-2026-Only"))
            });

        o.Auth.AccessToken.DeliveryMode = GrantDeliveryMode.Cookie;

        // TODO: Make multiple UAuthHub support via resolver, then different client apps can use different UAuthHub instances if needed.
        //o.Hub.BaseUrl = "https://localhost:6110"; // PureJwt auth mode doesn't need UAuthHub, but other auth modes do. This is the URL of the UAuthHub instance that this resource API trusts.
        // o.UAuthHubBaseUrl = "https://localhost:7213"; // Comment upper line and uncomment this if you want to try blazor server sample.

        // Standalone WASM
        o.Cors.AllowedOrigins.Add("https://localhost:6130");

        // Blazor Server
        o.Cors.AllowedOrigins.Add("https://localhost:7213");
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseHttpsRedirection();

app.UseUltimateAuthResourceApi();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var result = await context.AuthenticateAsync("UltimateAuth");

    Console.WriteLine(
        $"AUTH: Success={result.Succeeded}, " +
        $"Failure={result.Failure?.Message}, " +
        $"PrincipalAuthenticated={result.Principal?.Identity?.IsAuthenticated}, " +
        $"ContextAuthenticated={context.User.Identity?.IsAuthenticated}, " +
        $"Admin={context.User.IsInRole("Admin")}");

    await next(context);

    Console.WriteLine($"FINAL STATUS: {context.Response.StatusCode}");
});

app.UseAuthorization();

app.MapControllers();
app.Run();

using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
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
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256]
    };

    o.Cryptography.JwtVerificationKeys.Add(
        new UAuthJwtVerificationKeyOptions
        {
            KeyId = "uauth-hs256-v1",
            // Development only. Production must use secure key management.
            Key = Convert.ToBase64String(Encoding.UTF8.GetBytes("UltimateAuth-Development-JWT-Signing-Key-2026-Only"))
        });

    o.Hub.BaseUrl = "https://localhost:6112";
    o.Cors.AllowedOrigins.Add("https://localhost:6132");
    o.Cors.PolicyName = "resourceApi";
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseUltimateAuthResourceApiWithAspNetCore();

app.MapControllers();

app.Run();

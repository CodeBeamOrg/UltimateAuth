using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.Buffers.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var keyBytes = Convert.FromBase64String("VUxUSU1BVEVBVVRIX0RFVl9KV1RfU0lHTklOR19LRVlfVjE=");

var verificationKey = new SymmetricSecurityKey(keyBytes)
{
    KeyId = "uauth-hs256-v1"
};

builder.Services.AddSingleton<IJwtVerificationKeyProvider>(
    new StaticJwtVerificationKeyProvider(
        new[] { verificationKey }));

builder.Services.AddUltimateAuthResourceApi(o =>
    {
        // TODO: Make multiple UAuthHub support via resolver, then different client apps can use different UAuthHub instances if needed.
        o.UAuthHubBaseUrl = "https://localhost:6110";
        // o.UAuthHubBaseUrl = "https://localhost:7213"; // Comment upper line and uncomment this if you want to try blazor server sample.

        // Standalone WASM
        o.AllowedClientOrigins.Add("https://localhost:6130");

        // Blazor Server
        o.AllowedClientOrigins.Add("https://localhost:7213");
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

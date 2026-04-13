using CodeBeam.UltimateAuth.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddUltimateAuthResourceApi(o =>
    {
        // TODO: Make multiple UAuthHub support via resolver, then different client apps can use different UAuthHub instances if needed.
        o.UAuthHubBaseUrl = "https://localhost:6110";
        o.AllowedClientOrigins.Add("https://localhost:6130");
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

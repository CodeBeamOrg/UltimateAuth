using CodeBeam.UltimateAuth.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddUltimateAuthResourceApi(o =>
{
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

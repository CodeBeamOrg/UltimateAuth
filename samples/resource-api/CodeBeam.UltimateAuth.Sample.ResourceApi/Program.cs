using System.Security.Claims;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Server.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddUltimateAuthResourceApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("WasmSample", policy =>
    {
        policy
            .WithOrigins("https://localhost:6130")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("WasmSample");

app.UseUltimateAuthResourceApi();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () =>
{
    return Results.Ok(new
    {
        service = "ResourceApi",
        status = "ok"
    });
});

app.MapGet("/me", (ClaimsPrincipal user) =>
{
    return Results.Ok(new
    {
        IsAuthenticated = user.Identity?.IsAuthenticated,
        Name = user.Identity?.Name,
        Claims = user.Claims.Select(c => new
        {
            c.Type,
            c.Value
        })
    });
})
.RequireAuthorization();

app.MapGet("/data", () =>
{
    return Results.Ok(new
    {
        Message = "You are authorized to access protected data."
    });
})
.RequireAuthorization("ApiUser");

app.Run();

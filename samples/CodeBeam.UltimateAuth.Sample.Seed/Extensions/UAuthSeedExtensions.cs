using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Sample.Seed.Extensions;

public static class UAuthSeedExtensions
{
    public static async Task SeedUltimateAuthAsync(this WebApplication app, TenantKey? tenant = null, CancellationToken ct = default)
    {
        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();
        await runner.RunAsync(tenant, ct);
    }
}

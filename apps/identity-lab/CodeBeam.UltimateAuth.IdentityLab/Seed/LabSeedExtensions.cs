using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.IdentityLab.Seed;

public static class LabSeedExtensions
{
    public static async Task SeedIdentityLabAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<SeedRunner>();

        await runner.RunAsync(null, cancellationToken);
    }
}

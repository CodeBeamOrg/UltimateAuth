using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class SeedRunner
{
    private readonly IEnumerable<ISeedContributor> _contributors;

    public SeedRunner(IEnumerable<ISeedContributor> contributors)
    {
        _contributors = contributors;

        foreach (var c in contributors)
        {
            Console.WriteLine($"- {c.GetType().FullName}");
        }
    }

    public async Task RunAsync(TenantKey? tenant, CancellationToken ct = default)
    {
        if (tenant == null)
            tenant = TenantKey.Single;

        foreach (var c in _contributors.OrderBy(x => x.Order))
        {
            await c.SeedAsync((TenantKey)tenant, ct);
        }
    }
}

using CodeBeam.UltimateAuth.Core.Abstractions;

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

    public async Task RunAsync(string? tenantId, CancellationToken ct = default)
    {
        foreach (var c in _contributors.OrderBy(x => x.Order))
        {
            await c.SeedAsync(tenantId, ct);
        }
    }
}

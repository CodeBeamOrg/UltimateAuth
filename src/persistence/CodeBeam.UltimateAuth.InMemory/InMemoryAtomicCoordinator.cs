namespace CodeBeam.UltimateAuth.InMemory;

internal sealed class InMemoryAtomicCoordinator
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
}

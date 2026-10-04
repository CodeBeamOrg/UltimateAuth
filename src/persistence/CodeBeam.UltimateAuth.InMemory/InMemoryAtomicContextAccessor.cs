namespace CodeBeam.UltimateAuth.InMemory;

public sealed class InMemoryAtomicContextAccessor
{
    private readonly AsyncLocal<InMemoryAtomicContext?> _current = new();

    public InMemoryAtomicContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}

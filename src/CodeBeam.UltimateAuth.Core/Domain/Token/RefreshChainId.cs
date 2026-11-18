namespace CodeBeam.UltimateAuth.Core.Domain;

/// <summary>
/// Identifies a sequence of refresh tokens produced through rotation.
/// Independent of the session chain.
/// </summary>
public readonly record struct RefreshChainId(Guid Value)
{
    public static RefreshChainId New() => new(Guid.NewGuid());

    public static RefreshChainId From(Guid value)
        => value == Guid.Empty
            ? throw new ArgumentException("RefreshChainId cannot be empty.", nameof(value))
            : new RefreshChainId(value);

    public override string ToString() => Value.ToString("N");
}

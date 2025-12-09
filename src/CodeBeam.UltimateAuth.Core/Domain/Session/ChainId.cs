namespace CodeBeam.UltimateAuth.Core.Domain
{
    public readonly struct ChainId : IEquatable<ChainId>
    {
        public ChainId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public static ChainId New() => new ChainId(Guid.NewGuid());

        public bool Equals(ChainId other) => Value.Equals(other.Value);
        public override bool Equals(object? obj) => obj is ChainId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
        public static implicit operator Guid(ChainId id) => id.Value;
    }
}

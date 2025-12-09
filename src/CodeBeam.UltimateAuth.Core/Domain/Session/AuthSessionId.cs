namespace CodeBeam.UltimateAuth.Core.Domain
{
    public readonly struct AuthSessionId : IEquatable<AuthSessionId>
    {
        public AuthSessionId(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }

        public static AuthSessionId New() => new AuthSessionId(Guid.NewGuid());

        public bool Equals(AuthSessionId other) => Value.Equals(other.Value);
        public override bool Equals(object? obj) => obj is AuthSessionId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();

        public override string ToString() => Value.ToString();
        public static implicit operator Guid(AuthSessionId id) => id.Value;
    }
}

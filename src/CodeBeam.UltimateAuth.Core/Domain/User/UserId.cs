namespace CodeBeam.UltimateAuth.Core.Domain
{
    /// <summary>
    /// Strongly typed identifier for a user.
    /// Default user id implementation for UltimateAuth.
    /// </summary>
    public readonly record struct UserId(string Value)
    {
        public override string ToString() => Value;

        public static UserId New() => new(Guid.NewGuid().ToString("N"));

        public static implicit operator string(UserId id) => id.Value;
        public static implicit operator UserId(string value) => new(value);
    }
}

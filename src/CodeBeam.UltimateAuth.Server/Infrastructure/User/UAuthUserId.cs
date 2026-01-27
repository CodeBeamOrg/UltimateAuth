namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public readonly record struct UAuthUserId(Guid Value)
    {
        public override string ToString() => Value.ToString("N");

        public static UAuthUserId New() => new(Guid.NewGuid());

        public static implicit operator Guid(UAuthUserId id) => id.Value;
        public static implicit operator UAuthUserId(Guid value) => new(value);
    }
}

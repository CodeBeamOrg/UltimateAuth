namespace CodeBeam.UltimateAuth.Core.Domain
{
    public readonly record struct SessionRootId(Guid Value)
    {
        public static SessionRootId New() => new(Guid.NewGuid());

        public static SessionRootId From(Guid value)
            => value == Guid.Empty
                ? throw new ArgumentException("SessionRootId cannot be empty.", nameof(value))
                : new SessionRootId(value);

        public static bool TryCreate(string raw, out SessionRootId id)
        {
            if (Guid.TryParse(raw, out var guid) && guid != Guid.Empty)
            {
                id = new SessionRootId(guid);
                return true;
            }

            id = default;
            return false;
        }

        public override string ToString() => Value.ToString("N");
    }
}

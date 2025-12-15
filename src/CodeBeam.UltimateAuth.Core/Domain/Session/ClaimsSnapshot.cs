namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class ClaimsSnapshot
    {
        private readonly IReadOnlyDictionary<string, string> _claims;

        public ClaimsSnapshot(IReadOnlyDictionary<string, string> claims)
        {
            _claims = new Dictionary<string, string>(claims);
        }

        public IReadOnlyDictionary<string, string> AsDictionary() => _claims;

        public bool TryGet(string type, out string value) => _claims.TryGetValue(type, out value);

        public string? Get(string type)
            => _claims.TryGetValue(type, out var value)
                ? value
                : null;

        public static ClaimsSnapshot Empty { get; } = new ClaimsSnapshot(new Dictionary<string, string>());

        public override bool Equals(object? obj)
        {
            if (obj is not ClaimsSnapshot other)
                return false;

            if (_claims.Count != other._claims.Count)
                return false;

            foreach (var kv in _claims)
            {
                if (!other._claims.TryGetValue(kv.Key, out var v))
                    return false;

                if (!string.Equals(kv.Value, v, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                foreach (var kv in _claims.OrderBy(x => x.Key))
                {
                    hash = hash * 23 + kv.Key.GetHashCode();
                    hash = hash * 23 + kv.Value.GetHashCode();
                }
                return hash;
            }
        }

        public static ClaimsSnapshot From(params (string Type, string Value)[] claims)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (type, value) in claims)
                dict[type] = value;

            return new ClaimsSnapshot(dict);
        }

        // TODO: Add ToClaimsPrincipal and FromClaimsPrincipal methods

    }
}

using System.Security.Claims;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class ClaimsSnapshot
    {
        public IReadOnlyDictionary<string, string> Claims { get; }

        [JsonConstructor]
        public ClaimsSnapshot(IReadOnlyDictionary<string, string> claims)
        {
            Claims = new Dictionary<string, string>(claims);
        }

        public IReadOnlyDictionary<string, string> AsDictionary() => Claims;

        public bool TryGet(string type, out string value) => Claims.TryGetValue(type, out value);

        public string? Get(string type)
            => Claims.TryGetValue(type, out var value)
                ? value
                : null;

        public static ClaimsSnapshot Empty { get; } = new ClaimsSnapshot(new Dictionary<string, string>());

        public override bool Equals(object? obj)
        {
            if (obj is not ClaimsSnapshot other)
                return false;

            if (Claims.Count != other.Claims.Count)
                return false;

            foreach (var kv in Claims)
            {
                if (!other.Claims.TryGetValue(kv.Key, out var v))
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
                foreach (var kv in Claims.OrderBy(x => x.Key))
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

        public ClaimsSnapshot With(params (string Type, string Value)[] claims)
        {
            if (claims.Length == 0)
                return this;

            var dict = new Dictionary<string, string>(Claims, StringComparer.Ordinal);

            foreach (var (type, value) in claims)
            {
                dict[type] = value;
            }

            return new ClaimsSnapshot(dict);
        }

        public ClaimsSnapshot Merge(ClaimsSnapshot other)
        {
            if (other is null || other.Claims.Count == 0)
                return this;

            if (Claims.Count == 0)
                return other;

            var dict = new Dictionary<string, string>(Claims, StringComparer.Ordinal);

            foreach (var kv in other.Claims)
            {
                dict[kv.Key] = kv.Value;
            }

            return new ClaimsSnapshot(dict);
        }

        public static ClaimsSnapshot FromClaimsPrincipal(ClaimsPrincipal principal)
        {
            if (principal is null)
                return Empty;

            if (principal.Identity?.IsAuthenticated != true)
                return Empty;

            var dict = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var claim in principal.Claims)
            {
                dict[claim.Type] = claim.Value;
            }

            return new ClaimsSnapshot(dict);
        }

        public ClaimsPrincipal ToClaimsPrincipal(string authenticationType = "UltimateAuth")
        {
            if (Claims.Count == 0)
                return new ClaimsPrincipal(new ClaimsIdentity());

            var claims = Claims.Select(kv => new Claim(kv.Key, kv.Value));
            var identity = new ClaimsIdentity(claims, authenticationType);

            return new ClaimsPrincipal(identity);
        }

    }
}

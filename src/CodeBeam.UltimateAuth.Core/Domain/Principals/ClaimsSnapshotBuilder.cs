using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class ClaimsSnapshotBuilder
    {
        private readonly Dictionary<string, HashSet<string>> _claims = new(StringComparer.Ordinal);

        public ClaimsSnapshotBuilder Add(string type, string value)
        {
            if (!_claims.TryGetValue(type, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                _claims[type] = set;
            }

            set.Add(value);
            return this;
        }

        public ClaimsSnapshotBuilder AddMany(string type, IEnumerable<string> values)
        {
            foreach (var v in values)
                Add(type, v);

            return this;
        }

        public ClaimsSnapshotBuilder AddRole(string role) => Add(ClaimTypes.Role, role);

        public ClaimsSnapshotBuilder AddPermission(string permission) => Add("uauth:permission", permission);

        public ClaimsSnapshot Build()
        {
            var frozen = _claims.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value.ToArray(), StringComparer.Ordinal);
            return new ClaimsSnapshot(frozen);
        }
    }
}

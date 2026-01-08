using System.Security.Claims;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Extensions
{
    public static class ClaimsSnapshotExtensions
    {
        public static ClaimsPrincipal ToClaimsPrincipal(this ClaimsSnapshot snapshot, string authenticationType = "UltimateAuth")
        {
            var claims = snapshot
                .AsDictionary()
                .Select(kv => new Claim(kv.Key, kv.Value));

            var identity = new ClaimsIdentity(claims, authenticationType);
            return new ClaimsPrincipal(identity);
        }

        public static IReadOnlyCollection<Claim> ToClaims(this ClaimsSnapshot snapshot)
        {
            if (snapshot == null)
                return Array.Empty<Claim>();

            return snapshot
                .AsDictionary()
                .Select(kv => new Claim(kv.Key, kv.Value))
                .ToArray();
        }

        public static ClaimsSnapshot ToSnapshot(this IEnumerable<Claim> claims)
        {
            if (claims == null)
                return ClaimsSnapshot.Empty;

            return new ClaimsSnapshot(
                claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Last().Value,
                        StringComparer.Ordinal
                    )
            );
        }
    }
}

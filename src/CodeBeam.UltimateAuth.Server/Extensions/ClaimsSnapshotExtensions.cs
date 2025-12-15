using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Extensions
{
    public static class ClaimsSnapshotExtensions
    {
        public static IReadOnlyCollection<Claim> AsClaims(
            this ClaimsSnapshot snapshot)
            => snapshot.AsDictionary()
                .Select(kv => new Claim(kv.Key, kv.Value))
                .ToArray();
    }
}

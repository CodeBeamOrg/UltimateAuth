using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Extensions;

public static class ClaimsSnapshotExtensions
{
    /// <summary>
    /// Converts a ClaimsSnapshot into an ASP.NET Core ClaimsPrincipal.
    /// </summary>
    public static ClaimsPrincipal ToClaimsPrincipal(this ClaimsSnapshot snapshot, string authenticationType = "UltimateAuth")
    {
        if (snapshot == null)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = snapshot.Claims.SelectMany(kv => kv.Value.Select(value => new Claim(kv.Key, value)));

        var identity = new ClaimsIdentity(claims, authenticationType);
        return new ClaimsPrincipal(identity);
    }

    public static ClaimsPrincipal ToClaimsPrincipal(this ClaimsSnapshot snapshot, UserKey? userKey, string authenticationType)
    {
        if (snapshot == null)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = snapshot.Claims.SelectMany(kv => kv.Value.Select(v => new Claim(kv.Key, v))).ToList();

        if (userKey is not null)
        {
            var value = userKey.Value.ToString();
            claims.Add(new Claim(ClaimTypes.Name, value));
            claims.Add(new Claim(ClaimTypes.NameIdentifier, value));
        }

        var identity = new ClaimsIdentity(claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }


    /// <summary>
    /// Converts an ASP.NET Core ClaimsPrincipal into a ClaimsSnapshot.
    /// </summary>
    public static ClaimsSnapshot ToClaimsSnapshot(this ClaimsPrincipal principal)
    {
        if (principal is null)
            return ClaimsSnapshot.Empty;

        if (principal.Identity?.IsAuthenticated != true)
            return ClaimsSnapshot.Empty;

        var dict = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!dict.TryGetValue(claim.Type, out var set))
            {
                set = new HashSet<string>(StringComparer.Ordinal);
                dict[claim.Type] = set;
            }

            set.Add(claim.Value);
        }

        return new ClaimsSnapshot(dict.ToDictionary(kv => kv.Key, kv => (IReadOnlyCollection<string>)kv.Value.ToArray(), StringComparer.Ordinal));
    }

    public static IEnumerable<Claim> ToClaims(this ClaimsSnapshot snapshot)
    {
        foreach (var (type, values) in snapshot.Claims)
        {
            foreach (var value in values)
            {
                yield return new Claim(type, value);
            }
        }
    }

}

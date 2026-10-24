using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record SandboxUserItem(
    UserKey UserKey,
    string DisplayName,
    string Username,
    string Email,
    bool IsOwner,
    UserStatus Status,
    DateTimeOffset CreatedAt)
{
    public string Initials => string.Concat(DisplayName
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(x => char.ToUpperInvariant(x[0])));
}
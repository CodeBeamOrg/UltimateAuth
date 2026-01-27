using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record class UserLifecycle
{
    public UserKey UserKey { get; init; } = default!;

    public UserStatus Status { get; set; } = UserStatus.Active;

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

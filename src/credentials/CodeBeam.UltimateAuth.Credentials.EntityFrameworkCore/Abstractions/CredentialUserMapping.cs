namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

internal sealed class CredentialUserMapping<TUser, TUserId>
{
    public Func<TUser, TUserId> UserId { get; init; } = default!;
    public Func<TUser, string> Username { get; init; } = default!;
    public Func<TUser, string> PasswordHash { get; init; } = default!;
    public Func<TUser, long> SecurityVersion { get; init; } = default!;
    public Func<TUser, bool> CanAuthenticate { get; init; } = default!;
}

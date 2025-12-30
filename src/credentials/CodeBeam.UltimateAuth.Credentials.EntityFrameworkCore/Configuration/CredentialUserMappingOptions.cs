using System.Linq.Expressions;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public sealed class CredentialUserMappingOptions<TUser, TUserId>
{
    internal Expression<Func<TUser, TUserId>>? UserId { get; private set; }
    internal Expression<Func<TUser, string>>? Username { get; private set; }
    internal Expression<Func<TUser, string>>? PasswordHash { get; private set; }
    internal Expression<Func<TUser, long>>? SecurityVersion { get; private set; }
    internal Expression<Func<TUser, bool>>? CanAuthenticate { get; private set; }

    public void MapUserId(Expression<Func<TUser, TUserId>> expr) => UserId = expr;
    public void MapUsername(Expression<Func<TUser, string>> expr) => Username = expr;
    public void MapPasswordHash(Expression<Func<TUser, string>> expr) => PasswordHash = expr;
    public void MapSecurityVersion(Expression<Func<TUser, long>> expr) => SecurityVersion = expr;

    /// <summary>
    /// Optional. If not specified, all users are allowed to authenticate.
    /// Use this to enforce custom user state rules (e.g. Active, Locked, Suspended).
    /// Users that can't authenticate don't show up in authentication results.
    /// </summary>
    public void MapCanAuthenticate(Expression<Func<TUser, bool>> expr) => CanAuthenticate = expr;

    internal void ApplyUserId(Expression<Func<TUser, TUserId>> expr) => UserId = expr;
    internal void ApplyUsername(Expression<Func<TUser, string>> expr) => Username = expr;
    internal void ApplyPasswordHash(Expression<Func<TUser, string>> expr) => PasswordHash = expr;
    internal void ApplySecurityVersion(Expression<Func<TUser, long>> expr) => SecurityVersion = expr;
}

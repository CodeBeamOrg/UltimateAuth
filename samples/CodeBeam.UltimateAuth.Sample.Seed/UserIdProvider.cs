using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sample.Seed;

public sealed class UserIdProvider : IUserIdProvider<UserKey>
{
    private static readonly UserKey Admin = UserKey.FromGuid(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly UserKey User = UserKey.FromGuid(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    public UserKey GetAdminUserId() => Admin;
    public UserKey GetUserUserId() => User;
}

//using CodeBeam.UltimateAuth.Users;

//namespace CodeBeam.UltimateAuth.Users.InMemory;

//public sealed class InMemoryUserStore<TUserId> : IUserStore<TUserId>
//{
//    private readonly Dictionary<TUserId, User<TUserId>> _users = new();
//    private readonly Dictionary<TUserId, UserSecurityState> _security = new();

//    public void Add(User<TUserId> user)
//    {
//        _users[user.UserId] = user;
//        _security[user.UserId] = new UserSecurityState(0, false, false);
//    }

//    public Task<IUser<TUserId>?> FindByIdAsync(
//        string? tenantId,
//        TUserId userId,
//        CancellationToken cancellationToken = default)
//    {
//        _users.TryGetValue(userId, out var user);
//        return Task.FromResult<IUser<TUserId>?>(user);
//    }

//    public Task<IUserSecurityState> GetSecurityStateAsync(
//        string? tenantId,
//        TUserId userId,
//        CancellationToken cancellationToken = default)
//    {
//        return Task.FromResult<IUserSecurityState>(
//            _security[userId]);
//    }

//    public Task IncrementSecurityVersionAsync(
//        string? tenantId,
//        TUserId userId,
//        CancellationToken cancellationToken = default)
//    {
//        var current = _security[userId];
//        _security[userId] = current with
//        {
//            SecurityVersion = current.SecurityVersion + 1
//        };
//        return Task.CompletedTask;
//    }
//}

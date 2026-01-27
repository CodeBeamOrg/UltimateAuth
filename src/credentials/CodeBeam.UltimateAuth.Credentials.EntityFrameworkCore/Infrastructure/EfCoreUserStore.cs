//using CodeBeam.UltimateAuth.Core.Abstractions;
//using CodeBeam.UltimateAuth.Core.Domain;
//using CodeBeam.UltimateAuth.Core.Infrastructure;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Options;

//namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

//internal sealed class EfCoreUserStore<TUser, TUserId> : IUAuthUserStore<TUserId> where TUser : class
//{
//    private readonly DbContext _db;
//    private readonly CredentialUserMapping<TUser, TUserId> _map;

//    public EfCoreUserStore(DbContext db, IOptions<CredentialUserMappingOptions<TUser, TUserId>> options)
//    {
//        _db = db;
//        _map = CredentialUserMappingBuilder.Build(options.Value);
//    }

//    public async Task<IAuthSubject<TUserId>?> FindByIdAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
//    {
//        var user = await _db.Set<TUser>().FirstOrDefaultAsync(u => _map.UserId(u)!.Equals(userId), ct);

//        if (user is null || !_map.CanAuthenticate(user))
//            return null;

//        return new EfCoreAuthUser<TUserId>(_map.UserId(user));
//    }

//    public async Task<UserRecord<TUserId>?> FindByUsernameAsync(string? tenantId, string username, CancellationToken ct = default)
//    {
//        var user = await _db.Set<TUser>().FirstOrDefaultAsync(u => _map.Username(u) == username, ct);

//        if (user is null || !_map.CanAuthenticate(user))
//            return null;

//        return new UserRecord<TUserId>
//        {
//            Id = _map.UserId(user),
//            Username = _map.Username(user),
//            PasswordHash = _map.PasswordHash(user),
//            IsActive = true,
//            CreatedAt = DateTimeOffset.UtcNow,
//            IsDeleted = false
//        };
//    }

//    public async Task<IAuthSubject<TUserId>?> FindByLoginAsync(string? tenantId, string login, CancellationToken ct = default)
//    {
//        var user = await _db.Set<TUser>().FirstOrDefaultAsync(u => _map.Username(u) == login, ct);

//        if (user is null || !_map.CanAuthenticate(user))
//            return null;

//        return new EfCoreAuthUser<TUserId>(_map.UserId(user));
//    }

//    public Task<string?> GetPasswordHashAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
//    {
//        return _db.Set<TUser>()
//            .Where(u => _map.UserId(u)!.Equals(userId))
//            .Select(u => _map.PasswordHash(u))
//            .FirstOrDefaultAsync(ct);
//    }

//    public Task SetPasswordHashAsync(string? tenantId, TUserId userId, string passwordHash, CancellationToken token = default)
//    {
//        throw new NotSupportedException("Password updates are not supported by EfCoreUserStore. " +
//            "Use application-level user management services.");
//    }

//    public async Task<long> GetSecurityVersionAsync(string? tenantId, TUserId userId, CancellationToken ct = default)
//    {
//        var user = await _db.Set<TUser>().FirstOrDefaultAsync(u => _map.UserId(u)!.Equals(userId), ct);
//        return user is null ? 0 : _map.SecurityVersion(user);
//    }

//    public Task IncrementSecurityVersionAsync(string? tenantId, TUserId userId, CancellationToken token = default)
//    {
//        throw new NotSupportedException("Security version updates must be handled by the application.");
//    }

//}

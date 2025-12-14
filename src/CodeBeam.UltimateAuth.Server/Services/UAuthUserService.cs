using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;
using CodeBeam.UltimateAuth.Core.Users;

namespace CodeBeam.UltimateAuth.Server.Users;

internal sealed class UAuthUserService<TUserId> : IUAuthUserService<TUserId>
{
    private readonly IUAuthUserStore<TUserId> _userStore;
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly IUserIdFactory<TUserId> _userIdFactory;

    public UAuthUserService(
        IUAuthUserStore<TUserId> userStore,
        IUAuthPasswordHasher passwordHasher,
        IUserIdFactory<TUserId> userIdFactory)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _userIdFactory = userIdFactory;
    }

    public async Task<TUserId> RegisterAsync(
    RegisterUserRequest request,
    CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Identifier))
            throw new ArgumentException("Username is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Password is required.");

        if (await _userStore.ExistsByUsernameAsync(request.Identifier, ct))
            throw new InvalidOperationException("User already exists.");

        var hash = _passwordHasher.Hash(request.Password);

        var userId = _userIdFactory.Create();

        await _userStore.CreateAsync(
            new UserRecord<TUserId>
            {
                Id = userId,
                Username = request.Identifier,
                PasswordHash = hash,
                CreatedAt = DateTime.UtcNow
            },
            ct);

        return userId;
    }

    public async Task<bool> ValidateCredentialsAsync(
        ValidateCredentialsRequest request,
        CancellationToken ct = default)
    {
        var user = await _userStore.FindByUsernameAsync(request.Identifier, ct);
        if (user is null)
            return false;

        return _passwordHasher.Verify(
            request.Password,
            user.PasswordHash);
    }

    public Task DeleteAsync(
        TUserId userId,
        CancellationToken ct = default)
    {
        return _userStore.DeleteAsync(userId, ct);
    }
}

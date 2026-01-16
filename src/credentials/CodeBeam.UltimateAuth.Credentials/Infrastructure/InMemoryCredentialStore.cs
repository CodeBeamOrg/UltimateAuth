namespace CodeBeam.UltimateAuth.Credentials.InMemory;

public sealed class InMemoryCredentialStore<TUserId>
    : ICredentialStore<TUserId>
{
    private readonly Dictionary<string, ICredential<TUserId>> _byLogin = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<TUserId, List<ICredential<TUserId>>> _byUser = new();

    public void Add(ICredential<TUserId> credential)
    {
        if (credential is ILoginCredential<TUserId> login)
            _byLogin[login.LoginIdentifier] = credential;

        if (!_byUser.TryGetValue(credential.UserId, out var list))
        {
            list = new List<ICredential<TUserId>>();
            _byUser[credential.UserId] = list;
        }

        list.Add(credential);
    }

    public Task<ICredential<TUserId>?> FindByLoginAsync(
        string? tenantId,
        string loginIdentifier,
        CancellationToken cancellationToken = default)
    {
        _byLogin.TryGetValue(loginIdentifier, out var credential);
        return Task.FromResult(credential);
    }

    public Task<IReadOnlyCollection<ICredential<TUserId>>> GetByUserAsync(
        string? tenantId,
        TUserId userId,
        CancellationToken cancellationToken = default)
    {
        _byUser.TryGetValue(userId, out var list);
        return Task.FromResult<IReadOnlyCollection<ICredential<TUserId>>>(
            list ?? new List<ICredential<TUserId>>());
    }

    public Task UpdateSecretAsync(
        string? tenantId,
        TUserId userId,
        CredentialType type,
        string newSecretHash,
        CancellationToken cancellationToken = default)
    {
        // intentionally simple – reference implementation
        return Task.CompletedTask;
    }
}

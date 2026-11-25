using System.Threading;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Stores a single access token in managed memory.
/// </summary>
/// <remarks>
/// The instance must be scoped to the current client or user.
/// It does not persist credentials across application restarts.
/// </remarks>
internal sealed class InMemoryUAuthTokenStore : IUAuthTokenStore
{
    private AccessToken? _token;

    public AccessToken? Read()
        => Volatile.Read(ref _token);

    public void Replace(AccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);
        Volatile.Write(ref _token, token);
    }

    public void Clear()
        => Volatile.Write(ref _token, null);
}

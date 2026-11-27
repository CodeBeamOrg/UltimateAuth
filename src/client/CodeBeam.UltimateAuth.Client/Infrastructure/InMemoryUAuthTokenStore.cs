using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class InMemoryUAuthTokenStore : IUAuthTokenStore
{
    private readonly object _sync = new();

    private UAuthTokenSnapshot _snapshot = new(null, 0, 0);

    public AccessToken? Read() => Capture().Token;

    public UAuthTokenSnapshot Capture() => Volatile.Read(ref _snapshot);

    public void Replace(AccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        lock (_sync)
        {
            var current = _snapshot;

            Volatile.Write(
                ref _snapshot,
                new UAuthTokenSnapshot(token, current.Generation + 1, current.Epoch + 1));
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            var current = _snapshot;

            Volatile.Write(
                ref _snapshot,
                new UAuthTokenSnapshot(null, current.Generation + 1, current.Epoch + 1));
        }
    }

    public bool TryRotate(UAuthTokenSnapshot expected, AccessToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        lock (_sync)
        {
            var current = _snapshot;

            if (current.Epoch != expected.Epoch || current.Generation != expected.Generation)
            {
                return false;
            }

            Volatile.Write(
                ref _snapshot,
                new UAuthTokenSnapshot(token, current.Generation + 1, current.Epoch));

            return true;
        }
    }

    public bool TryClear(UAuthTokenSnapshot expected)
    {
        lock (_sync)
        {
            var current = _snapshot;

            if (current.Epoch != expected.Epoch || current.Generation != expected.Generation)
            {
                return false;
            }

            Volatile.Write(
                ref _snapshot,
                new UAuthTokenSnapshot(null, current.Generation + 1, current.Epoch + 1));

            return true;
        }
    }
}

namespace CodeBeam.UltimateAuth.Client.Abstractions;

/// <summary>Stores the access credential for the current client scope.</summary>
public interface IUAuthTokenStore
{
    UAuthAccessToken? Read();
    void Replace(UAuthAccessToken token);
    void Clear();
}

/// <summary>An access credential and the expiration supplied by its issuer.</summary>
public sealed class UAuthAccessToken(string value, DateTimeOffset expiresAt)
{
    public string Value { get; } = value;
    public DateTimeOffset ExpiresAt { get; } = expiresAt;
}

internal sealed class InMemoryUAuthTokenStore : IUAuthTokenStore
{
    private UAuthAccessToken? _token;

    public UAuthAccessToken? Read() => Volatile.Read(ref _token);
    public void Replace(UAuthAccessToken token) => Volatile.Write(ref _token, token);
    public void Clear() => Volatile.Write(ref _token, null);
}

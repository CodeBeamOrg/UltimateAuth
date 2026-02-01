namespace CodeBeam.UltimateAuth.Core.Domain;

// AuthSessionId is a opaque token, because it's more sensitive data. SessionChainId and SessionRootId are Guid.
public readonly record struct AuthSessionId
{
    public string Value { get; }

    private AuthSessionId(string value)
    {
        Value = value;
    }

    public static bool TryCreate(string raw, out AuthSessionId id)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            id = default;
            return false;
        }

        if (raw.Length < 32)
        {
            id = default;
            return false;
        }

        id = new AuthSessionId(raw);
        return true;
    }

    public override string ToString() => Value;

    public static implicit operator string(AuthSessionId id) => id.Value;
}

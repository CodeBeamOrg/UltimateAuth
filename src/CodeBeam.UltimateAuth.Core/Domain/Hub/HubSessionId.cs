namespace CodeBeam.UltimateAuth.Core.Domain;

// TODO: Bind id with IP and UA
public readonly record struct HubSessionId
{
    public string Value { get; }

    private HubSessionId(string value)
    {
        Value = value;
    }

    public static HubSessionId New() => new(Guid.NewGuid().ToString("N"));

    public static HubSessionId Parse(string value)
    {
        if (!TryParse(value, out var id))
            throw new FormatException("Invalid HubSessionId.");

        return id;
    }

    public static bool TryParse(string? value, out HubSessionId sessionId)
    {
        sessionId = default;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!Guid.TryParseExact(value, "N", out _))
            return false;

        sessionId = new HubSessionId(value);
        return true;
    }

    public override string ToString() => Value;
}

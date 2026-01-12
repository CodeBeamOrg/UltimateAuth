namespace CodeBeam.UltimateAuth.Core.Domain;

// TODO: Bind id with IP and UA
public readonly record struct HubSessionId(string Value)
{
    public static HubSessionId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;

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
}

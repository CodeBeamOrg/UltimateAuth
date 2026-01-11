namespace CodeBeam.UltimateAuth.Core.Domain;

// TODO: Bind id with IP and UA
public readonly record struct HubSessionId(string Value)
{
    public static HubSessionId New() => new(Guid.NewGuid().ToString("N"));

    public override string ToString() => Value;
}

namespace CodeBeam.UltimateAuth.Authorization.Domain;

public readonly record struct Permission(string Value)
{
    public override string ToString() => Value;
}

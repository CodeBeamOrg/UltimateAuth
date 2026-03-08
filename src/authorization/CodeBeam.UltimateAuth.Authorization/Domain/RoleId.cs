namespace CodeBeam.UltimateAuth.Authorization.Domain;

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New() => new(Guid.NewGuid());

    public static RoleId From(Guid guid) => new(guid);

    public override string ToString() => Value.ToString();
}

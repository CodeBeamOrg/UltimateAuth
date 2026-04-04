namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public readonly record struct RoleId(Guid Value) : IParsable<RoleId>
{
    public static RoleId New() => new(Guid.NewGuid());

    public static RoleId From(Guid guid) => new(guid);

    public static RoleId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s));

    public static bool TryParse(string? s, IFormatProvider? provider, out RoleId result)
    {
        if (Guid.TryParse(s, out var guid))
        {
            result = new RoleId(guid);
            return true;
        }

        result = default;
        return false;
    }

    public override string ToString() => Value.ToString();
}

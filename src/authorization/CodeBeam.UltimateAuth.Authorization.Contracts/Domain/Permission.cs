namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public readonly record struct Permission(string Value)
{
    public static Permission From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("permission_required");

        return new Permission(value.Trim().ToLowerInvariant());
    }

    public static readonly Permission Wildcard = new("*");

    public override string ToString() => Value;
}

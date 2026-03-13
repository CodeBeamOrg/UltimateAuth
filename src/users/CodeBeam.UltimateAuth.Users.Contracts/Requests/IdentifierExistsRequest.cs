namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class IdentifierExistsRequest
{
    public UserIdentifierType Type { get; set; }
    public string Value { get; set; } = default!;
}

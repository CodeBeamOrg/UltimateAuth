namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record SetPrimaryUserIdentifierRequest
    {
        public UserIdentifierType Type { get; init; }
        public string Value { get; init; } = default!;
    }
}

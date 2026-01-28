namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record UpdateUserIdentifierRequest
    {
        public UserIdentifierType Type { get; init; }
        public string OldValue { get; init; } = default!;
        public string NewValue { get; init; } = default!;
    }
}

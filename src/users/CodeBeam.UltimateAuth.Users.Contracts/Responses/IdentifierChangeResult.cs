namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record IdentifierChangeResult
    {
        public bool Succeeded { get; init; }
        public string? FailureReason { get; init; }

        public static IdentifierChangeResult Success() => new() { Succeeded = true };

        public static IdentifierChangeResult Failed(string reason) => new() { Succeeded = false, FailureReason = reason };
    }
}

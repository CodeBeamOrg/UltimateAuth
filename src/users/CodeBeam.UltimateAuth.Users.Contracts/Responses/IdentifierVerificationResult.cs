namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record IdentifierVerificationResult
    {
        public bool Succeeded { get; init; }
        public string? FailureReason { get; init; }

        public static IdentifierVerificationResult Success() => new() { Succeeded = true };

        public static IdentifierVerificationResult Failed(string reason) => new() { Succeeded = false, FailureReason = reason };
    }
}

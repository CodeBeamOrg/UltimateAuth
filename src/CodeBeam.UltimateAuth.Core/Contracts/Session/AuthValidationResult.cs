namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthValidationResult
    {
        public bool IsValid { get; init; }
        public string? State { get; init; }
        public int? RemainingAttempts { get; init; }

        public AuthStateSnapshot? Snapshot { get; init; }

        public static AuthValidationResult Valid(AuthStateSnapshot? snapshot = null)
        => new()
        {
            IsValid = true,
            State = "active",
            Snapshot = snapshot
        };

        public static AuthValidationResult Invalid(string state)
            => new()
            {
                IsValid = false,
                State = state
            };
    }
}

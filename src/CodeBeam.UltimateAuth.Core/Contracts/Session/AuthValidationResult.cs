namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthValidationResult
    {
        public bool IsValid { get; init; }
        public string? State { get; init; }

        public int? RemainingAttempts { get; init; }

        public static AuthValidationResult Valid() => new() { IsValid = true, State = "active" };

        public static AuthValidationResult Invalid(string state) => new() { IsValid = false, State = state };
    }
}

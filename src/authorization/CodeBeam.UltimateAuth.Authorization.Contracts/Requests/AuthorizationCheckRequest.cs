namespace CodeBeam.UltimateAuth.Authorization.Contracts
{
    public sealed class AuthorizationCheckRequest
    {
        public required string Action { get; init; }
        public string? Resource { get; init; }
        public string? ResourceId { get; init; }
    }
}

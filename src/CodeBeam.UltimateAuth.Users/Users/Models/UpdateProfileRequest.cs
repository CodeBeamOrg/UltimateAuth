namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class UpdateProfileRequest
    {
        public string? Username { get; init; }
        public string? Email { get; init; }
    }
}

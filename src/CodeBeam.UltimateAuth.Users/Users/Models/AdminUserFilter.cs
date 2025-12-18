namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class AdminUserFilter
    {
        public bool? IsActive { get; init; }
        public bool? IsEmailConfirmed { get; init; }

        public string? Search { get; init; }
    }
}

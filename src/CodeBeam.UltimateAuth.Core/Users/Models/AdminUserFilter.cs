namespace CodeBeam.UltimateAuth.Core.Users.Models
{
    public sealed class AdminUserFilter
    {
        public bool? IsActive { get; init; }
        public bool? IsEmailConfirmed { get; init; }

        public string? Search { get; init; }
    }
}

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class DeleteProfileRequest
{
    public required ProfileKey ProfileKey { get; init; }
}

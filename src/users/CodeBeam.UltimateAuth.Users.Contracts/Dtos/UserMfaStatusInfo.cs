namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record UserMfaStatusInfo
{
    public bool IsEnabled { get; init; }
    public IReadOnlyCollection<MfaMethod> EnabledMethods { get; init; } = Array.Empty<MfaMethod>();
    public MfaMethod? DefaultMethod { get; init; }
}

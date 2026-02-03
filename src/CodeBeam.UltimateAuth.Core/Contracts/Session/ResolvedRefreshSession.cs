using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record ResolvedRefreshSession
{
    public bool IsValid { get; init; }
    public bool IsReuseDetected { get; init; }

    public UAuthSession? Session { get; init; }
    public UAuthSessionChain? Chain { get; init; }

    private ResolvedRefreshSession() { }

    public static ResolvedRefreshSession Invalid()
        => new()
        {
            IsValid = false
        };

    public static ResolvedRefreshSession Reused()
        => new()
        {
            IsValid = false,
            IsReuseDetected = true
        };

    public static ResolvedRefreshSession Valid(UAuthSession session, UAuthSessionChain chain)
        => new()
        {
            IsValid = true,
            Session = session,
            Chain = chain
        };
}

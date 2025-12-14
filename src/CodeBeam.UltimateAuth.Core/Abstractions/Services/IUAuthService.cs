namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// High-level facade for UltimateAuth.
    /// Provides access to authentication flows,
    /// session lifecycle and user operations.
    /// </summary>
    public interface IUAuthService<TUserId>
    {
        IUAuthFlowService Flow { get; }
        IUAuthSessionService<TUserId> Sessions { get; }
        IUAuthTokenService<TUserId> Tokens { get; }
        IUAuthUserService<TUserId> Users { get; }
    }
}

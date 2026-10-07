namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum RefreshTokenPersistence
{
    /// <summary>
    /// Refresh token persists to the store.
    /// </summary>
    Persist = 0,

    /// <summary>
    /// Refresh token does not persist to the store.
    /// </summary>
    DoNotPersist = 10
}

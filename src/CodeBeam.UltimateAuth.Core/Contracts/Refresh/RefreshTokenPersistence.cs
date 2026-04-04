namespace CodeBeam.UltimateAuth.Core.Contracts;

public enum RefreshTokenPersistence
{
    /// <summary>
    /// Refresh token store'a yazılır.
    /// Login, first-issue gibi normal akışlar için.
    /// </summary>
    Persist = 0,

    /// <summary>
    /// Refresh token store'a yazılmaz.
    /// Rotation gibi özel akışlarda,
    /// caller tarafından kontrol edilir.
    /// </summary>
    DoNotPersist = 10
}

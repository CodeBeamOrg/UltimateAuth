namespace CodeBeam.UltimateAuth.Client.Abstractions;

public interface ISessionEvents
{
    /// <summary>
    /// Fired when the current session becomes invalid
    /// due to revoke or security mismatch.
    /// </summary>
    event Action? CurrentSessionRevoked;

    void RaiseCurrentSessionRevoked();
}

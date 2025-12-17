namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    /// <summary>
    /// Provides an abstracted time source for the system.
    /// Used to improve testability and ensure consistent time handling.
    /// </summary>
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }
    }
}

namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Provides an abstracted time source for the system.
/// Used to improve testability and ensure consistent time handling.
/// </summary>
// TODO: Add UnixTimeSeconds, TimeZone-aware Now, etc. if needed.
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Represents the result of an UltimateAuth session or token refresh operation.
/// </summary>
public sealed record RefreshResult
{
    /// <summary>
    /// Gets a value indicating whether the refresh operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Gets the status code associated with the refresh operation.
    /// </summary>
    public int Status { get; init; }

    /// <summary>
    /// Gets the semantic outcome of the refresh operation.
    /// </summary>
    public RefreshOutcome Outcome { get; init; }
}

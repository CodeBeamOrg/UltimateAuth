namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record AuthorizationRequest
{
    /// <summary>
    /// Logical operation being requested (e.g. "orders.read").
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Optional resource identifier (row, entity, aggregate, etc).
    /// </summary>
    public string? Resource { get; init; }

    /// <summary>
    /// Optional resource identifier.
    /// </summary>
    public string? ResourceId { get; init; }

    /// <summary>
    /// Optional contextual attributes for fine-grained access decisions.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Attributes { get; init; }
}

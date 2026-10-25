namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents a trusted, server-resolved role assigned to the authenticated actor during access evaluation.
/// </summary>
public sealed record AccessActorRole(string Id, string Name);

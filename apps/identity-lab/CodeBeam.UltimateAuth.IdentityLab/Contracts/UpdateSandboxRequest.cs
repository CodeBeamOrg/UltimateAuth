using CodeBeam.UltimateAuth.IdentityLab.Domain;

namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record UpdateSandboxRequest(string Name, int MaxUsers, SandboxStatus Status);

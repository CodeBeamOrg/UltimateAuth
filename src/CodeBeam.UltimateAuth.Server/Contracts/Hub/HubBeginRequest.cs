using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Contracts;

public sealed record HubBeginRequest
{
    public string AuthorizationCode { get; init; } = default!;
    public string CodeVerifier { get; init; } = default!;

    public UAuthClientProfile ClientProfile { get; init; }
    public TenantKey Tenant { get; init; }

    public string? ReturnUrl { get; init; }

    public string? PreviousHubSessionId { get; init; }

    public required DeviceContext Device { get; init; }
}

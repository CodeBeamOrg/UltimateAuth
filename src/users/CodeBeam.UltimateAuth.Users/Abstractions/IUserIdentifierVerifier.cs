using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

/// <summary>
/// Validates identifier ownership proof on the server.
/// </summary>
/// <remarks>
/// Implementations must bind the proof to the tenant, user,
/// identifier and current value; enforce expiry, attempt limits
/// and single-use consumption.
/// </remarks>
public interface IUserIdentifierVerifier
{
    Task<bool> VerifyAsync(UserIdentifierVerificationContext context, CancellationToken ct = default);
}

public sealed record UserIdentifierVerificationContext(
    TenantKey Tenant,
    UserKey UserKey,
    Guid IdentifierId,
    UserIdentifierType Type,
    string Value,
    string Proof);

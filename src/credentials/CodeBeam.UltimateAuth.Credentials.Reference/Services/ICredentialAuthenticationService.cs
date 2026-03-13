using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

/// <summary>
/// Orchestrates an authentication attempt against a credential type.
/// Responsible for applying lockout policy by mutating the credential aggregate
/// and persisting it.
/// </summary>
public interface ICredentialAuthenticationService
{
    //Task<CredentialAuthenticationResult> AuthenticateAsync(
    //    AccessContext context,
    //    CredentialType type,
    //    CredentialAuthenticationRequest request,
    //    CancellationToken ct = default);
}
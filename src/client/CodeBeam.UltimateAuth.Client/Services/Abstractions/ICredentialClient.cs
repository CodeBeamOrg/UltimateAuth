using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides credential management operations such as password creation, update, revocation, and reset flows.
/// </summary>
/// <remarks>
/// <para>
/// This client handles both self-service and administrative credential operations.
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>Credential creation and update</description></item>
/// <item><description>Credential revocation</description></item>
/// <item><description>Credential reset flows (begin/complete)</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Reset operations are typically multi-step (begin → complete).</description></item>
/// <item><description>Self methods operate on the current authenticated user.</description></item>
/// <item><description>User methods require administrative privileges.</description></item>
/// <item><description>Credential changes may invalidate active sessions depending on security policy.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface ICredentialClient
{
    /// <summary>
    /// Adds a new credential for the current user.
    /// </summary>
    /// <remarks>
    /// Typically used for initial password setup or adding alternative credentials.
    /// </remarks>
    Task<UAuthResult<AddCredentialResult>> AddMyAsync(AddCredentialRequest request);

    /// <summary>
    /// Changes the credential of the current user.
    /// </summary>
    /// <remarks>
    /// May require the current credential for verification depending on policy.
    /// </remarks>
    Task<UAuthResult<ChangeCredentialResult>> ChangeMyAsync(ChangeCredentialRequest request);

    /// <summary>
    /// Revokes the current user's credential.
    /// </summary>
    /// <remarks>
    /// Revocation may invalidate active sessions.
    /// </remarks>
    Task<UAuthResult> RevokeMyAsync(RevokeCredentialRequest request);

    /// <summary>
    /// Starts the credential reset process for the current user.
    /// </summary>
    /// <remarks>
    /// This typically issues a verification step (e.g., email or OTP).
    /// </remarks>
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetMyAsync(BeginResetCredentialRequest request);

    /// <summary>
    /// Completes the credential reset process for the current user.
    /// </summary>
    /// <remarks>
    /// Must be called after a successful <see cref="BeginResetMyAsync"/>.
    /// </remarks>
    Task<UAuthResult<CredentialActionResult>> CompleteResetMyAsync(CompleteResetCredentialRequest request);


    /// <summary>
    /// Adds a credential for a specific user.
    /// </summary>
    /// <remarks>
    /// Requires administrative privileges.
    /// </remarks>
    Task<UAuthResult<AddCredentialResult>> AddUserAsync(UserKey userKey, AddCredentialRequest request);

    /// <summary>
    /// Changes the credential of a specific user.
    /// </summary>
    /// <remarks>
    /// Typically used for administrative resets or overrides.
    /// </remarks>
    Task<UAuthResult<ChangeCredentialResult>> ChangeUserAsync(UserKey userKey, ChangeCredentialRequest request);

    /// <summary>
    /// Revokes a credential for a specific user.
    /// </summary>
    Task<UAuthResult> RevokeUserAsync(UserKey userKey, RevokeCredentialRequest request);

    /// <summary>
    /// Starts the credential reset process for a specific user.
    /// </summary>
    Task<UAuthResult<BeginCredentialResetResult>> BeginResetUserAsync(UserKey userKey, BeginResetCredentialRequest request);

    /// <summary>
    /// Completes the credential reset process for a specific user.
    /// </summary>
    Task<UAuthResult<CredentialActionResult>> CompleteResetUserAsync(UserKey userKey, CompleteResetCredentialRequest request);

    /// <summary>
    /// Deletes a credential associated with a specific user.
    /// </summary>
    /// <remarks>
    /// This removes softly or permanently the credential from the system.
    /// </remarks>
    Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteCredentialRequest request);
}

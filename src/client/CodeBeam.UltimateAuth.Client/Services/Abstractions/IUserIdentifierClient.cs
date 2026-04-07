using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides operations for managing user identifiers such as email, username, and phone.
/// </summary>
/// <remarks>
/// <para>
/// Identifiers represent login and contact points for a user (e.g., email, username, phone).
/// Each identifier has a type, value, and verification state.
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>Add, update, and delete identifiers</description></item>
/// <item><description>Mark identifiers as primary</description></item>
/// <item><description>Verify identifiers (e.g., email or phone verification)</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Identifier values are normalized and must be unique per tenant.</description></item>
/// <item><description>Only one primary identifier per type is allowed.</description></item>
/// <item><description>Verification is required for sensitive operations depending on policy.</description></item>
/// <item><description>Self methods operate on the current user; user methods require administrative privileges.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IUserIdentifierClient
{
    /// <summary>
    /// Retrieves identifiers of the current user.
    /// </summary>
    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetMyAsync(PageRequest? request = null);

    /// <summary>
    /// Adds a new identifier to the current user.
    /// </summary>
    /// <remarks>
    /// The identifier must be unique within the tenant.
    /// </remarks>
    Task<UAuthResult> AddMyAsync(AddUserIdentifierRequest request);

    /// <summary>
    /// Updates an existing identifier of the current user.
    /// </summary>
    /// <remarks>
    /// May require re-verification depending on the change.
    /// </remarks>
    Task<UAuthResult> UpdateMyAsync(UpdateUserIdentifierRequest request);

    /// <summary>
    /// Marks an identifier as primary for the current user.
    /// </summary>
    /// <remarks>
    /// Only one primary identifier per type is allowed.
    /// </remarks>
    Task<UAuthResult> SetMyPrimaryAsync(SetPrimaryUserIdentifierRequest request);

    /// <summary>
    /// Removes the primary designation from an identifier.
    /// </summary>
    /// <remarks>
    /// At least one primary identifier may be required depending on system policy.
    /// </remarks>
    Task<UAuthResult> UnsetMyPrimaryAsync(UnsetPrimaryUserIdentifierRequest request);

    /// <summary>
    /// Verifies an identifier of the current user.
    /// </summary>
    /// <remarks>
    /// Typically used for email or phone verification flows.
    /// </remarks>
    Task<UAuthResult> VerifyMyAsync(VerifyUserIdentifierRequest request);

    /// <summary>
    /// Deletes an identifier of the current user.
    /// </summary>
    /// <remarks>
    /// Primary identifiers may need to be reassigned before deletion.
    /// </remarks>
    Task<UAuthResult> DeleteMyAsync(DeleteUserIdentifierRequest request);


    /// <summary>
    /// Retrieves identifiers of a specific user.
    /// </summary>
    Task<UAuthResult<PagedResult<UserIdentifierInfo>>> GetUserAsync(UserKey userKey, PageRequest? request = null);

    /// <summary>
    /// Adds an identifier to a specific user.
    /// </summary>
    Task<UAuthResult> AddUserAsync(UserKey userKey, AddUserIdentifierRequest request);

    /// <summary>
    /// Updates an identifier of a specific user.
    /// </summary>
    Task<UAuthResult> UpdateUserAsync(UserKey userKey, UpdateUserIdentifierRequest request);

    /// <summary>
    /// Marks an identifier as primary for a specific user.
    /// </summary>
    Task<UAuthResult> SetUserPrimaryAsync(UserKey userKey, SetPrimaryUserIdentifierRequest request);

    /// <summary>
    /// Removes the primary designation from an identifier of a specific user.
    /// </summary>
    Task<UAuthResult> UnsetUserPrimaryAsync(UserKey userKey, UnsetPrimaryUserIdentifierRequest request);

    /// <summary>
    /// Verifies an identifier for a specific user.
    /// </summary>
    Task<UAuthResult> VerifyUserAsync(UserKey userKey, VerifyUserIdentifierRequest request);

    /// <summary>
    /// Deletes an identifier of a specific user.
    /// </summary>
    Task<UAuthResult> DeleteUserAsync(UserKey userKey, DeleteUserIdentifierRequest request);
}

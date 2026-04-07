using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

/// <summary>
/// Provides user management and profile operations for both self-service and administrative scenarios.
/// </summary>
/// <remarks>
/// <para>
/// This client handles user lifecycle operations as well as profile management.
/// </para>
///
/// <para>
/// UltimateAuth supports <b>multi-identifier</b> and <b>multi-profile</b> per user:
/// <list type="bullet">
/// <item><description>Each user can have multiple profiles (e.g., "default", "business"). You can enable it with server options.</description></item>
/// <item><description>Profile selection is controlled via <see cref="GetProfileRequest"/>.</description></item>
/// <item><description>If no profile is specified, the default profile is used.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Key capabilities:
/// <list type="bullet">
/// <item><description>User creation and deletion</description></item>
/// <item><description>User status management</description></item>
/// <item><description>Profile retrieval and updates</description></item>
/// <item><description>Multi-profile creation and deletion</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Self methods operate on the current authenticated user.</description></item>
/// <item><description>Admin methods require elevated permissions.</description></item>
/// <item><description>Deleting a user removes all associated profiles.</description></item>
/// <item><description>Default profile cannot be deleted.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IUserClient
{
    /// <summary>
    /// Queries users with filtering and pagination.
    /// </summary>
    Task<UAuthResult<PagedResult<UserSummary>>> QueryAsync(UserQuery query);

    /// <summary>
    /// Creates a new user as the current user context.
    /// </summary>
    /// <remarks>
    /// Behavior may vary depending on client permissions and configuration.
    /// Self creation may be allowed or restricted based on server settings.
    /// </remarks>
    Task<UAuthResult<UserCreateResult>> CreateAsync(CreateUserRequest request);

    /// <summary>
    /// Creates a new user with administrative privileges.
    /// </summary>
    Task<UAuthResult<UserCreateResult>> CreateAsAdminAsync(CreateUserRequest request);

    /// <summary>
    /// Changes the status of the current user.
    /// </summary>
    Task<UAuthResult<UserStatusChangeResult>> ChangeMyStatusAsync(ChangeUserStatusSelfRequest request);

    /// <summary>
    /// Changes the status of a specific user.
    /// </summary>
    Task<UAuthResult<UserStatusChangeResult>> ChangeUserStatusAsync(UserKey userKey, ChangeUserStatusAdminRequest request);

    /// <summary>
    /// Deletes the current user. This is a soft-delete operation. Only administrators can restore deleted users or permanently removes soft deleted users.
    /// </summary>
    /// <remarks>
    /// This operation removes all associated profiles and sessions.
    /// </remarks>
    Task<UAuthResult> DeleteMeAsync();

    /// <summary>
    /// Deletes a specific user.
    /// </summary>
    Task<UAuthResult<UserDeleteResult>> DeleteUserAsync(UserKey userKey, DeleteUserRequest request);


    /// <summary>
    /// Retrieves the current user's profile.
    /// </summary>
    /// <remarks>
    /// If <paramref name="request"/> is null, the default profile is returned.
    /// </remarks>
    Task<UAuthResult<UserView>> GetMeAsync(GetProfileRequest? request = null);

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    /// <remarks>
    /// The target profile is determined by <see cref="UpdateProfileRequest.ProfileKey"/>.
    /// </remarks>
    Task<UAuthResult> UpdateMeAsync(UpdateProfileRequest request);

    /// <summary>
    /// Creates a new profile for the current user.
    /// </summary>
    /// <remarks>
    /// Profile keys must be unique per user.
    /// Default profile is automatically created on user creation and cannot be duplicated.
    /// </remarks>
    Task<UAuthResult> CreateMyProfileAsync(CreateProfileRequest request);

    /// <summary>
    /// Deletes a profile of the current user.
    /// </summary>
    /// <remarks>
    /// The default profile cannot be deleted.
    /// </remarks>
    Task<UAuthResult> DeleteMyProfileAsync(ProfileKey profileKey);


    /// <summary>
    /// Retrieves a profile of a specific user.
    /// </summary>
    Task<UAuthResult<UserView>> GetUserAsync(UserKey userKey, GetProfileRequest? request = null);

    /// <summary>
    /// Updates a profile of a specific user.
    /// </summary>
    Task<UAuthResult> UpdateUserAsync(UserKey userKey, UpdateProfileRequest request);

    /// <summary>
    /// Creates a profile for a specific user.
    /// </summary>
    Task<UAuthResult> CreateUserProfileAsync(UserKey userKey, CreateProfileRequest request);

    /// <summary>
    /// Deletes a profile of a specific user.
    /// </summary>
    Task<UAuthResult> DeleteUserProfileAsync(UserKey userKey, ProfileKey profileKey);
}

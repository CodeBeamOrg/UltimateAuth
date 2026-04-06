using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Entry point for interacting with UltimateAuth from client applications.
/// Provides access to all authentication, user, session, and authorization operations.
/// </summary>
/// <remarks>
/// <para>
/// This client is designed to work across different client profiles (Blazor Server, WASM, MAUI, MVC, API).
/// Behavior may vary depending on the configured <c>ClientProfile</c>.
/// </para>
/// 
/// <para>
/// Key components:
/// <list type="bullet">
/// <item><description><see cref="Flows"/>: Handles login, logout, refresh and auth flows.</description></item>
/// <item><description><see cref="Sessions"/>: Manages session lifecycle and validation.</description></item>
/// <item><description><see cref="Users"/>: User profile and account operations.</description></item>
/// <item><description><see cref="Identifiers"/>: Email, username, phone management.</description></item>
/// <item><description><see cref="Credentials"/>: Password and credential operations.</description></item>
/// <item><description><see cref="Authorization"/>: Permission and policy checks.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Important:
/// <list type="bullet">
/// <item><description>Session-based flows may rely on cookies (Blazor Server) or tokens (WASM).</description></item>
/// <item><description>State changes (login, logout, profile updates) may trigger client events.</description></item>
/// <item><description>Multi-tenant behavior depends on client configuration.</description></item>
/// </list>
/// </para>
/// </remarks>
public interface IUAuthClient
{
    /// <summary>
    /// Provides authentication flow operations such as login, logout, and refresh.
    /// </summary>
    IFlowClient Flows { get; }

    /// <summary>
    /// Provides access to session lifecycle operations.
    /// </summary>
    ISessionClient Sessions { get; }

    /// <summary>
    /// Provides user profile and account management operations.
    /// </summary>
    IUserClient Users { get; }

    /// <summary>
    /// Manages user identifiers such as email, username, and phone.
    /// </summary>
    IUserIdentifierClient Identifiers { get; }

    /// <summary>
    /// Provides credential operations such as password management.
    /// </summary>
    ICredentialClient Credentials { get; }

    /// <summary>
    /// Provides authorization and policy evaluation operations.
    /// </summary>
    IAuthorizationClient Authorization { get; }
}

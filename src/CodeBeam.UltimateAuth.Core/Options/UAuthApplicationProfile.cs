namespace CodeBeam.UltimateAuth.Core.Options;

/// <summary>
/// Identifies the type of application participating in the UltimateAuth ecosystem.
/// </summary>
/// <remarks>
/// An application profile describes the application's runtime environment and its role in the authentication architecture.
/// It is used to select appropriate default authentication modes, credential delivery strategies and integration behavior.
/// <para>
/// Profiles are not authentication modes.
/// Different profiles may use the same authentication mode, and explicit configuration may override a profile's defaults.
/// </para>
/// <para>
/// A profile is not proof of application identity or trust.
/// Client-supplied profile information must not be treated as authenticated security evidence.
/// </para>
/// </remarks>
public enum UAuthApplicationProfile
{
    /// <summary>
    /// The application profile has not been identified or specified.
    /// </summary>
    /// <remarks>
    /// The hosting integration or registered profile resolver may determine the appropriate application profile.
    /// </remarks>
    NotSpecified = 0,

    /// <summary>
    /// A Blazor WebAssembly application running in a web browser.
    /// </summary>
    /// <remarks>
    /// Represents a browser-hosted client with no trusted server-side execution environment of its own.
    /// </remarks>
    BlazorWasm = 10,

    /// <summary>
    /// A Blazor Server application executing on an ASP.NET Core server.
    /// </summary>
    /// <remarks>
    /// Supports server-managed authentication state and session-based credential handling.
    /// The default authentication mode is <see cref="UAuthMode.PureOpaque"/>.
    /// </remarks>
    BlazorServer = 20,

    /// <summary>
    /// A .NET MAUI application running on a supported device platform.
    /// </summary>
    /// <remarks>
    /// Represents a native application that typically exchanges credentials through HTTP headers rather than browser cookies.
    /// </remarks>
    Maui = 30,

    /// <summary>
    /// A general-purpose server-hosted web application.
    /// </summary>
    /// <remarks>
    /// Intended for applications such as ASP.NET Core MVC, Razor Pages and other server-rendered web applications.
    /// </remarks>
    WebServer = 40,

    /// <summary>
    /// A backend application consuming authentication-protected
    /// services or APIs.
    /// </summary>
    /// <remarks>
    /// Represents an API consumer or service client.
    /// It does not imply that the application is acting as an UltimateAuth Resource Server.
    /// </remarks>
    Api = 50,

    /// <summary>
    /// An UltimateAuth Resource API protecting application resources.
    /// </summary>
    /// <remarks>
    /// Represents the Resource Server role.
    /// It validates incoming authentication credentials and enforces authorization for protected resources.
    /// <para>
    /// The default authentication mode is <see cref="UAuthMode.PureJwt"/>.
    /// An explicit mode or custom authentication mode resolver may select another supported mode.
    /// </para>
    /// </remarks>
    ResourceApi = 60,

    /// <summary>
    /// An UltimateAuth Hub responsible for centralized authentication and credential lifecycle operations.
    /// </summary>
    /// <remarks>
    /// Represents the authentication authority, including credential issuance, login, refresh and logout operations.
    /// It is distinct from a Resource Server and does not represent an ordinary API consumer.
    /// </remarks>
    UAuthHub = 100
}

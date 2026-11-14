using AwesomeAssertions;
using CodeBeam.UltimateAuth.Server.Security;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class CsrfEndpointCoverageTests
{
    [Fact]
    public void AllAuthenticationAndMutationEndpoints_MustDeclareCsrfProtection()
    {
        using var factory = new AuthServerFactory();

        // Ensure the application and endpoint routing are initialized.
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var endpoints = factory.Services
            .GetRequiredService<EndpointDataSource>()
            .Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.RoutePattern.RawText?.TrimStart('/').StartsWith("auth/", StringComparison.OrdinalIgnoreCase) == true)
            .ToArray();

        endpoints.Should().NotBeEmpty();

        var expected = new Dictionary<string, UAuthCsrfOperation>(StringComparer.OrdinalIgnoreCase)
        {
            // Authentication
            ["auth/login"] = UAuthCsrfOperation.Login,
            ["auth/try-login"] = UAuthCsrfOperation.Login,
            ["auth/pkce/complete"] = UAuthCsrfOperation.PkceCompletion,
            ["auth/pkce/try-complete"] = UAuthCsrfOperation.PkceCompletion,

            // Session and logout
            ["auth/logout"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/refresh"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/logout-device"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/logout-others"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/logout-all"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/sessions/revoke-others"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/sessions/revoke-all"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/sessions/chains/{chainId}/revoke"] = UAuthCsrfOperation.AuthenticatedMutation,

            // User lifecycle
            ["auth/me/status"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/delete"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/create"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/status"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Profiles
            ["auth/me/profile/create"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/profile/update"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/profile/delete"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/profile/create"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/profile/update"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/profile/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Self identifiers
            ["auth/me/identifiers/add"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/identifiers/update"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/identifiers/set-primary"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/identifiers/unset-primary"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/identifiers/verify"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/identifiers/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Admin identifiers
            ["auth/admin/users/{userKey}/identifiers/add"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/identifiers/update"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/identifiers/set-primary"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/identifiers/unset-primary"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/identifiers/verify"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/identifiers/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Self credentials
            ["auth/me/credentials/add"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/credentials/change"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/me/credentials/revoke"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Admin credentials
            ["auth/admin/users/{userKey}/credentials/add"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/credentials/change"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/credentials/revoke"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/credentials/reset/begin"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/credentials/reset/complete"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/credentials/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Authorization
            ["auth/admin/authorization/users/{userKey}/roles/assign"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/authorization/users/{userKey}/roles/remove"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/authorization/roles/create"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/authorization/roles/{roleId}/rename"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/authorization/roles/{roleId}/permissions"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/authorization/roles/{roleId}/delete"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Admin session operations
            ["auth/admin/users/{userKey}/logout-device"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/logout-others"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/logout-all"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/sessions/{sessionId}/revoke"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/sessions/chains/{chainId}/revoke"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/sessions/revoke-root"] = UAuthCsrfOperation.AuthenticatedMutation,
            ["auth/admin/users/{userKey}/sessions/revoke-all"] = UAuthCsrfOperation.AuthenticatedMutation,

            // Authentication security management
            ["auth/admin/users/{userKey}/authentication/unlock"] = UAuthCsrfOperation.AuthenticatedMutation,
        };

        var actual = endpoints
            .Where(e => e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods.Contains("POST") == true)
            .ToDictionary(
                e => e.RoutePattern.RawText!.TrimStart('/'),
                e => e.Metadata.GetMetadata<UAuthCsrfMetadata>()?.Operation,
                StringComparer.OrdinalIgnoreCase);

        // Every expected protected endpoint must exist and carry
        // the exact expected CSRF operation.
        foreach (var (route, operation) in expected)
        {
            actual.Should().ContainKey(route, $"protected endpoint {route} must be registered");
            actual[route].Should().Be(operation, $"endpoint {route} must declare {operation}");
        }

        // Explicitly unprotected POST endpoints must be reviewed.
        // These are read-only or anonymous flows with separate security semantics.
        var explicitlyExcluded = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "auth/validate",
            "auth/password-policy",
            "auth/pkce/authorize",
            "auth/users/create",
            "auth/me/credentials/reset/begin",
            "auth/me/credentials/reset/complete",

            "auth/me/sessions/chains",
            "auth/me/sessions/chains/{chainId}",
            "auth/admin/users/{userKey}/sessions/chains",
            "auth/admin/users/{userKey}/sessions/chains/{chainId}",

            "auth/admin/users/query",
            "auth/admin/users/get-by-keys",
            "auth/me/profile/get",
            "auth/admin/users/{userKey}/profile/get",
            "auth/me/identifiers/get",
            "auth/admin/users/{userKey}/identifiers/get",
            "auth/me/identifiers/check-availability",

            "auth/me/authorization/check",
            "auth/me/authorization/roles/get",
            "auth/me/authorization/permissions/get",
            "auth/admin/authorization/users/{userKey}/roles/get",
            "auth/admin/authorization/roles/query",

            "auth/admin/users/{userKey}/authentication/lockout/get",

            // UAuthHub bootstrap endpoint:
            // Creates a short-lived HubFlowArtifact only.
            // Does not authenticate the user or issue session credentials.
            // PKCE completion performs the actual authentication.
            "auth/uauthhub/entry",
        };

        var unexpected = actual.Keys
            .Except(expected.Keys, StringComparer.OrdinalIgnoreCase)
            .Except(explicitlyExcluded, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        unexpected.Should().BeEmpty(
            "new POST endpoints require an explicit CSRF security classification");

        // Exclusions must not silently acquire a different behavior.
        foreach (var route in explicitlyExcluded)
        {
            if (actual.TryGetValue(route, out var metadata))
            {
                metadata.Should().BeNull(
                    $"excluded endpoint {route} must be reviewed before changing its CSRF classification");
            }
        }
    }
}
